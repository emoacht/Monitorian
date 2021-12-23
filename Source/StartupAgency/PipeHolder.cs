using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StartupAgency
{
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

		private readonly Lazy<List<NamedPipeServerStream>> _servers = new(() => new());
		private readonly Lazy<CancellationTokenSource> _cts = new(() => new());

		/// <summary>
		/// Creates <see cref="System.Threading.Semaphore"/> to start named pipes.
		/// </summary>
		/// <param name="args">Arguments to another instance</param>
		/// <returns>
		///	<para>success: True if no other instance exists and this instance successfully creates</para>
		/// <para>response: Response from another instance if that instance exists and returns an response</para>
		/// </returns>
		public (bool success, string response) Create(string[] args)
		{
			_semaphore = new Semaphore(1, 1, SemaphoreName, out bool createdNew);
			if (createdNew)
			{
				// Start server.
				try
				{
					StartServer(_cts.Value.Token);
				}
				catch (Exception ex)
				{
					Trace.WriteLine("Named pipe server failed." + Environment.NewLine
						+ ex);
				}
				return (success: true, null);
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
				return (success: false, response);
			}
		}

		/// <summary>
		/// Releases <see cref="System.Threading.Semaphore"/> to stop named pipes.
		/// </summary>
		public void Release()
		{
			_cts.Value.Cancel();

			// List of servers is created only when server is started.
			if (_servers.IsValueCreated)
			{
				foreach (var server in _servers.Value)
					server.Dispose();

				_servers.Value.Clear();
			}

			_semaphore?.Dispose();
		}

		#region Server

		private void StartServer(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Message);
			_servers.Value.Add(server);

			server.WaitForConnectionAsync(cancellationToken)
				.ContinueWith(async _ => await HandleConnectionAsync(server, cancellationToken), cancellationToken);
		}

		private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			// Because this method is not awaited on main thread, exceptions must be caught within
			// this method.
			try
			{
				StartServer(cancellationToken);

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

				if (!server.IsConnected)
					return;

				using (var writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true })
				{
					await writer.WriteAsync(response).ConfigureAwait(false);

					server.WaitForPipeDrain();
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Named pipe server failed." + Environment.NewLine
					+ ex);
			}
			finally
			{
				server.Dispose();
				_servers.Value.Remove(server);
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
					// Filter out null bacause it causes NullReferenceException in WriteLineAsync method
					// on .NET Framework.
					// Filter out string.Empty because it is used to indicate the end of writing.
					foreach (var arg in args.Where(x => !string.IsNullOrEmpty(x)))
						await writer.WriteLineAsync(arg).ConfigureAwait(false);
				}

				// WriteLineAsync method (w/o value) writes a line terminater and when server read it
				// by ReadLineAsync method, it becomes string.Empty as the line terminater is removed.
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
}