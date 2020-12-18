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
		/// Creates <see cref="System.Threading.Semaphore"/> to start remoting.
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="args">Arguments to another instance</param>
		/// <returns>
		/// <para>success: True if no other instance exists and this instance successfully creates</para>
		/// <para>response: Response from another instance if that instance exists and returns an response</para>
		/// </returns>
		public (bool success, object response) Create(string name, string[] args)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			// Determine Semaphore name and IPC port name.
			var semaphoreName = $"semaphore-{name}";
			var ipcPortName = $"port-{name}";
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
					Debug.WriteLine("Failed to start remoting as server." + Environment.NewLine
						+ ex);
				}
				return (success: true, null);
			}
			else
			{
				object response = null;
				try
				{
					// Instantiate a client channel and register it.
					var client = new IpcClientChannel();
					ChannelServices.RegisterChannel(client, true);

					// Set a proxy for a remoting object instantiated by another instance.
					var ipcPath = $"ipc://{ipcPortName}/{ipcUri}";
					_space = Activator.GetObject(typeof(RemotingSpace), ipcPath) as RemotingSpace;

					// Request that instance to take an action. If it is older version, RemotingException
					// will be thrown because the remoting object has no such method.
					response = _space?.Request(args);
				}
				catch (RemotingException ex)
				{
					Debug.WriteLine("Failed to start remoting as client." + Environment.NewLine
						+ ex);
				}
				return (success: false, response);
			}
		}

		/// <summary>
		/// Releases <see cref="System.Threading.Semaphore"/> to end remoting.
		/// </summary>
		public void Release()
		{
			_semaphore?.Dispose();
		}

		public event EventHandler<StartupRequestEventArgs> Requested
		{
			add { if (_space is not null) { _space.Requested += value; } }
			remove { if (_space is not null) { _space.Requested -= value; } }
		}
	}

	internal class RemotingSpace : MarshalByRefObject
	{
		public override object InitializeLifetimeService() => null;

		public event EventHandler<StartupRequestEventArgs> Requested;

		/// <summary>
		/// Requests the instance which instantiated this object to take an action.
		/// </summary>
		/// <param name="args">Arguments to that instance</param>
		/// <returns>Response from that instance</returns>
		/// <remarks>This method is to be called by an instance other than that instance.</remarks>
		public object Request(string[] args)
		{
			var e = new StartupRequestEventArgs(args);
			Requested?.Invoke(this, e);
			return e.Response;
		}

		[Obsolete]
		public void RaiseShowRequested() => Request(null);
	}
}