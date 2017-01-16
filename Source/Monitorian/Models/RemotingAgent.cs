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

namespace Monitorian.Models
{
	public class RemotingAgent
	{
		private Semaphore _semaphore;
		private IpcServerChannel _server;
		private RemotingSpace _space;

		public event EventHandler ShowRequested
		{
			add { if (_space != null) { _space.ShowRequested += value; } }
			remove { if (_space != null) { _space.ShowRequested -= value; } }
		}

		/// <summary>
		/// Starts remoting.
		/// </summary>
		/// <returns>True if no other instance there and this instance started remoting server. False if not.</returns>
		public bool Start()
		{
			// Determine Semaphore name and IPC port name using assembly title.
			var title = ProductInfo.Title ?? "Titleless";
			var semaphoreName = $"semaphore-{title}";
			var ipcPortName = $"port-{title}";
			const string ipcUri = "space";

			bool createdNew;
			_semaphore = new Semaphore(1, 1, semaphoreName, out createdNew);
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
					_space?.ShowRequest();
				}
				catch (RemotingException ex)
				{
					Debug.WriteLine("Failed to start remoting." + Environment.NewLine
						+ ex);
				}
				return false;
			}
		}

		public void End()
		{
			_semaphore?.Dispose();
		}
	}

	public class RemotingSpace : MarshalByRefObject
	{
		public event EventHandler ShowRequested;

		public void ShowRequest() => ShowRequested?.Invoke(this, null);

		public override object InitializeLifetimeService() => null;
	}
}