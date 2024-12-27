using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
	///	<para>started: True if this instance successfully starts the named pipe server</para>
	/// <para>response: Response from another instance if that instance exists and returns an response</para>
	/// </returns>
	public (bool created, bool started, string response) CreateAndStart(string[] args)
	{
		_semaphore = new Semaphore(1, 1, SemaphoreName, out bool createdNew);
		if (createdNew)
		{
			// Start server.
			try
			{
				StartServer(_cts.Value.Token);

				return (created: true, started: true, null);
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
				response = ConnectServerAsync(args, TimeSpan.FromSeconds(3), _cts.Value.Token).Result;
			}
			catch (Exception ex) // AggregateException
			{
				Trace.WriteLine("Named pipe client failed." + Environment.NewLine
					+ ex);
			}
			return (created: false, started: false, response);
		}
	}

	/// <summary>
	/// Releases <see cref="System.Threading.Semaphore"/> to stop named pipes.
	/// </summary>
	public void Release()
	{
		_cts.Value.Cancel();
		_semaphore?.Dispose();
	}

	#region Server

	private void StartServer(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		// Try starting a named pipe server.
		NamedPipeServerStream server = null;
		try
		{
			server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
		}
		catch (UnauthorizedAccessException)
		{
			// This exception is thrown if a named pipe server of the same path has already started.
			throw;
		}
		finally
		{
			server?.Dispose();
		}

		Task.Run(async () =>
		{
			while (true)
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				if (!await WaitAndConnectClientAsync(cancellationToken))
					break;
			}
		}, cancellationToken);
	}

	private async Task<bool> WaitAndConnectClientAsync(CancellationToken cancellationToken)
	{
		// As this method is not awaited on main thread, exceptions must be caught within
		// this method.

		NamedPipeServerStream server = null;
		try
		{
			try
			{
				server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			}
			catch (UnauthorizedAccessException)
			{
				// This exception is thrown if a named pipe server of the same path has already started.
				return false;
			}

			try
			{
				await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				return false;
			}

			var buffer = new List<string>();
			using (var reader = new StreamReader(server, Encoding.UTF8, true, 1024, leaveOpen: true))
			{
				while (true)
				{
					var value = await reader.ReadLineAsync().ConfigureAwait(false);
					// Check if value is string.Empty which indicates the end of writing.
					if (string.IsNullOrEmpty(value))
						break;

					buffer.Add(value);
				}
			}

			var response = await (HandleRequestAsync?.Invoke(buffer.ToArray()) ?? Task.FromResult<string>(null));

			if (server.IsConnected)
			{
				using (var writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true })
				{
					await writer.WriteAsync(response).ConfigureAwait(false);

					server.WaitForPipeDrain();
				}
			}
			return true;
		}
		finally
		{
			server?.Dispose();
		}
	}

	#endregion

	#region Client

	private async Task<string> ConnectServerAsync(string[] args, TimeSpan timeout, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			return null;

		using var client = new NamedPipeClientStream(PipeName);

		try
		{
			await client.ConnectAsync((int)timeout.TotalMilliseconds, cancellationToken).ConfigureAwait(false);
		}
		catch (TimeoutException)
		{
			return null;
		}

		using (var writer = new StreamWriter(client, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true })
		{
			if (args is { Length: > 0 })
			{
				// Filter out null because it causes NullReferenceException in WriteLineAsync
				// method on .NET Framework.
				// Filter out string.Empty because it is used to indicate the end of writing.
				foreach (var arg in args.Where(x => !string.IsNullOrEmpty(x)))
					await writer.WriteLineAsync(arg).ConfigureAwait(false);
			}

			// WriteLineAsync method (w/o value) writes a line terminator and when server reads it
			// by ReadLineAsync method, it becomes string.Empty as the line terminator is removed.
			// It is used to inform server of the end of writing.
			await writer.WriteLineAsync().ConfigureAwait(false);
		}

		if (!client.IsConnected)
			return null;

		using (var reader = new StreamReader(client, Encoding.UTF8))
		{
			return await reader.ReadToEndAsync().ConfigureAwait(false);
		}
	}

	#endregion
}