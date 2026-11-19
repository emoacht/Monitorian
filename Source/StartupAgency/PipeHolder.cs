using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StartupAgency;

internal class PipeHolder
{
	private readonly string _name;
	// Named pipes in packaged applications must use the syntax \\.\pipe\LOCAL\ for the pipe name.
	// https://docs.microsoft.com/en-us/windows/uwp/communication/interprocess-communication#pipes
	private string PipeName => $@"\\.\pipe\LOCAL\{_name}";
	private string SemaphoreName => $"semaphore-{_name}";

	public Func<string[], Task<string>> HandleRequestAsync { get; set; }

	public PipeHolder(string name, Func<string[], Task<string>> handleRequestAsync)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this._name = name;
		this.HandleRequestAsync = handleRequestAsync;
	}

	private Semaphore _semaphore;

	private readonly Lazy<CancellationTokenSource> _cts = new(() => new());

	/// <summary>
	/// Creates <see cref="System.Threading.Semaphore"/> and starts named pipes.
	/// </summary>
	/// <param name="args">Arguments being forwarded to another instance</param>
	/// <returns>
	///	<para>created: True if no other instance exists and this instance successfully creates the semaphore</para>
	///	<para>started: True if this instance successfully starts a named pipe server</para>
	/// <para>response: Response from another instance if that instance exists and returns a response</para>
	/// </returns>
	public (bool created, bool started, string response) Create(string[] args)
	{
		_semaphore = new Semaphore(1, 1, SemaphoreName, out bool createdNew);
		if (createdNew)
		{
			// Start server.
			try
			{
				var success = StartServer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), _cts.Value.Token);

				return (created: true, started: success, null);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Named pipe server failed." + Environment.NewLine
					+ ex);

				return (created: true, started: false, null);
			}
		}
		else
		{
			// Start client.
			string response = null;
			try
			{
				response = StartClient(args, TimeSpan.FromSeconds(3), _cts.Value.Token);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Named pipe client failed." + Environment.NewLine
					+ ex);
			}
			return (created: false, started: false, response);
		}
	}

	/// <summary>
	/// Releases <see cref="System.Threading.Semaphore"/> and stops named pipes.
	/// </summary>
	public void Release()
	{
		_cts.Value.Cancel();
		_semaphore?.Dispose();
	}

	private static string GetSidHash()
	{
		using var wi = WindowsIdentity.GetCurrent();
		var sid = wi.User ?? throw new InvalidOperationException("SID is null.");
		var data = Encoding.UTF8.GetBytes(sid.Value);

		using var sha256 = SHA256.Create();
		var hash = sha256.ComputeHash(data);

		// Truncate to 8 bytes. That is enough to avoid collisions between users.
		return BitConverter.ToString(hash, 0, 8).Replace("-", "");
	}

	private enum PipeResult
	{
		None = 0,
		Success,
		Canceled,
		Unauthorized,
		Unconnected,
		Disconnected
	}

	#region Server

	private readonly Lazy<PipeSecurity> _pipeSecurity = new(() => GetSecurity());

	private static PipeSecurity GetSecurity()
	{
		using var wi = WindowsIdentity.GetCurrent();
		var sid = wi.User ?? throw new InvalidOperationException("SID is null.");
		var security = new PipeSecurity();

		// Allow only the current user.
		security.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
		return security;
	}

	private bool TryCreateServer(string pipeName, out NamedPipeServerStream server)
	{
		try
		{
			server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
				0, 0, _pipeSecurity.Value);
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			// This exception is thrown if a named pipe server of the same path has already started.
			server = null;
			return false;
		}
	}

	private bool StartServer(TimeSpan timeout, TimeSpan interval, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			return false;

		// Start the initial server.
		var pipeName = PipeName;
		if (!TryCreateServer(pipeName, out var server))
		{
			pipeName += GetSidHash();
			if (!TryCreateServer(pipeName, out server))
				return false;
		}

		Task.Run(async () =>
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var result = await WaitAndConnectClientAsync(server, timeout, cancellationToken);
						switch (result)
						{
							case PipeResult.Canceled:
								return;

							case PipeResult.Unconnected:
							case PipeResult.Disconnected:
								// If named pipe is unconnected or disconnected unexpectedly, leave an interval.
								try
								{
									await Task.Delay(interval, cancellationToken);
								}
								catch (TaskCanceledException)
								{
									return;
								}
								break;
						}
					}
					finally
					{
						server?.Dispose();
						server = null;
					}

					// Start the subsequent server.
					if (!TryCreateServer(pipeName, out server))
						return;
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Named pipe server failed." + Environment.NewLine
					+ ex);
				throw;
			}
			finally
			{
				// In case CancellationToken is canceled before while loop.
				server?.Dispose();
			}
		}); // fire-and-forget

		return true;
	}

	private async Task<PipeResult> WaitAndConnectClientAsync(NamedPipeServerStream server, TimeSpan timeout, CancellationToken cancellationToken)
	{
		try
		{
			await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return PipeResult.Canceled;
		}
		catch (IOException)
		{
			return PipeResult.Unconnected;
		}

		var buffer = new List<string>();
		try
		{
			using (var reader = new StreamReader(server, Encoding.UTF8, true, 1024, leaveOpen: true))
			{
				while (true)
				{
					// If no newline char is found, ReadLineAsync method will wait indefinitely.
					var readTask = reader.ReadLineAsync();
					var completedTask = await Task.WhenAny(readTask, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
					if (completedTask != readTask)
					{
						// If timed out, close underlying stream to release it.
						server.Close();

						try
						{
							// Closing underlying stream during operation may cause an exception.
							await readTask;
						}
						catch
						{
						}

						if (cancellationToken.IsCancellationRequested)
						{
							Trace.WriteLine("Named pipe read operation is canceled.");
							return PipeResult.Canceled;
						}
						Trace.WriteLine("Named pipe read operation timed out.");
						break;
					}

					var value = readTask.Result;
					// Check if value is string.Empty which indicates the end of writing.
					if (string.IsNullOrEmpty(value))
						break;

					buffer.Add(value);
				}
			}
		}
		catch (IOException)
		{
			return PipeResult.Disconnected;
		}

		var response = await (HandleRequestAsync?.Invoke(buffer.ToArray()) ?? Task.FromResult<string>(null));

		if (!server.IsConnected)
			return PipeResult.Disconnected;

		try
		{
			using (var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = true })
			{
				await writer.WriteAsync(response).ConfigureAwait(false);
				server.WaitForPipeDrain();
			}
		}
		catch (IOException)
		{
			return PipeResult.Disconnected;
		}

		return PipeResult.Success;
	}

	#endregion

	#region Client

	private string StartClient(string[] args, TimeSpan timeout, CancellationToken cancellationToken)
	{
		var pipeName = PipeName;
		var (ended, response) = Connect();
		if (ended)
			return response;

		pipeName += GetSidHash();
		(_, response) = Connect();
		return response;

		(bool ended, string response) Connect()
		{
			var (result, buffer) = ConnectServerAsync(pipeName, args, timeout, cancellationToken).GetAwaiter().GetResult();
			return result switch
			{
				PipeResult.Success or
				PipeResult.Canceled or
				PipeResult.Disconnected => (true, buffer),
				_ => (false, null)
			};
		}
	}

	private async Task<(PipeResult result, string response)> ConnectServerAsync(string pipeName, string[] args, TimeSpan timeout, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			return (PipeResult.Canceled, null);

		using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

		try
		{
			await client.ConnectAsync((int)timeout.TotalMilliseconds, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return (PipeResult.Canceled, null);
		}
		catch (UnauthorizedAccessException)
		{
			// This exception is typically thrown when attempting to connect to a named pipe server that
			// has the same path but belongs to another user.
			return (PipeResult.Unauthorized, null);
		}
		catch (TimeoutException)
		{
			return (PipeResult.Unconnected, null);
		}

		try
		{
			using (var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true })
			{
				if (args is { Length: > 0 })
				{
					// Filter out null because it will cause NullReferenceException in WriteLineAsync
					// method on .NET Framework.
					// Filter out string.Empty because it is used to indicate the end of writing.
					// Newline chars would confuse ReadLineAsync method, which relies on them to detect
					// line endings, but they are not expected here.
					foreach (var arg in args.Where(x => !string.IsNullOrEmpty(x)))
						await writer.WriteLineAsync(arg).ConfigureAwait(false);
				}

				// WriteLineAsync method (w/o value) writes a line terminator and when server reads it
				// by ReadLineAsync method, it becomes string.Empty as the line terminator is removed.
				// It is used to inform server of the end of writing.
				await writer.WriteLineAsync().ConfigureAwait(false);
			}
		}
		catch (IOException)
		{
			return (PipeResult.Disconnected, null);
		}

		if (!client.IsConnected)
			return (PipeResult.Disconnected, null);

		try
		{
			using (var reader = new StreamReader(client, Encoding.UTF8))
			{
				var response = await reader.ReadToEndAsync().ConfigureAwait(false);
				return (PipeResult.Success, response);
			}
		}
		catch (IOException)
		{
			return (PipeResult.Disconnected, null);
		}
	}

	#endregion
}