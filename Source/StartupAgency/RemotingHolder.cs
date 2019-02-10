using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StartupAgency
{
	internal class RemotingHolder
	{
		private Semaphore _semaphore;
		private IpcServerChannel _server;
		private RemotingSpace _space;

		/// <summary>
		/// Creates semaphore to start remoting.
		/// </summary>
		/// <returns>True if no other instance exists and this instance instantiated remoting server</returns>
		public bool Create(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentNullException(nameof(title));

			// Determine Semaphore name and IPC port name using assembly title.
			var semaphoreName = $"semaphore-{title}";
			var ipcPortName = $"port-{title}";
			const string ipcUri = "space";

			_semaphore = new Semaphore(1, 1, semaphoreName, out bool createdNew);
			if (createdNew)
			{
				try
				{
					// Instantiate a server channel and register it.
					_server = new IpcServerChannel(ipcPortName);
					ChannelServices.RegisterChannel(_server, true);

					// Instantiate a remoting object and publish it.
					_space = new RemotingSpace();
					RemotingServices.Marshal(_space, ipcUri, typeof(RemotingSpace));
				}
				catch (RemotingException ex)
				{
					Debug.WriteLine("Failed to start remoting." + Environment.NewLine
						+ ex);
				}
				return true;
			}
			else
			{
				try
				{
					// Instantiate a client channel and register it.
					var client = new IpcClientChannel();
					ChannelServices.RegisterChannel(client, true);

					// Set a proxy for remote object.
					var ipcPath = $"ipc://{ipcPortName}/{ipcUri}";
					_space = Activator.GetObject(typeof(RemotingSpace), ipcPath) as RemotingSpace;

					// Raise event.
					_space?.RaiseShowRequested();
				}
				catch (RemotingException ex)
				{
					Debug.WriteLine("Failed to start remoting." + Environment.NewLine
						+ ex);
				}
				return false;
			}
		}

		/// <summary>
		/// Releases semaphore to end remoting.
		/// </summary>
		public void Release()
		{
			_semaphore?.Dispose();
		}

		public event EventHandler ShowRequested
		{
			add { if (_space != null) { _space.ShowRequested += value; } }
			remove { if (_space != null) { _space.ShowRequested -= value; } }
		}
	}

	internal class RemotingSpace : MarshalByRefObject
	{
		public override object InitializeLifetimeService() => null;

		public event EventHandler ShowRequested;

		/// <summary>
		/// Raise event.
		/// </summary>
		/// <remarks>
		/// This method is intended to be called by an instance other than that instantiated this object.
		/// </remarks>
		public void RaiseShowRequested()
		{
			ShowRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}