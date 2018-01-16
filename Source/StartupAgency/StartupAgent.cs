using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using StartupAgency.Helper;
using StartupAgency.Worker;
using StartupBridge;

namespace StartupAgency
{
	/// <summary>
	/// Startup agent
	/// </summary>
	public class StartupAgent : IDisposable
	{
		private IStartupWorker _worker;
		private RemotingHolder _holder;

		/// <summary>
		/// Starts functions.
		/// </summary>
		/// <param name="startupTaskId">Startup task ID</param>
		/// <param name="caller">Caller assembly</param>
		/// <remarks>Startup task ID must match that in AppxManifest.xml.</remarks>
		public bool Start(string startupTaskId, Assembly caller = null)
		{
			if (string.IsNullOrWhiteSpace(startupTaskId))
				throw new ArgumentNullException(nameof(startupTaskId));

			if (caller == null)
				caller = Assembly.GetCallingAssembly();

			var title = caller.GetTitle();

			_holder = new RemotingHolder();
			if (!_holder.Create(title))
				return false;

			_worker = (OsVersion.Is10Redstone1OrNewer && PlatformInfo.IsPackaged)
				? (IStartupWorker)new BridgeWorker(taskId: startupTaskId)
				: (IStartupWorker)new RegistryWorker(title: title, path: caller.Location);
			return true;
		}

		#region IDisposable

		private bool _isDisposed = false;

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				_holder?.Release();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion

		/// <summary>
		/// Occurs when caller instance is requested to show its existence by another instance.
		/// </summary>
		/// <remarks>
		/// This event will be raised by a thread other than that instantiated this object.
		/// Accordingly, the appropriate use of dispatcher is required.
		/// </remarks>
		public event EventHandler ShowRequested
		{
			add { if (_holder != null) { _holder.ShowRequested += value; } }
			remove { if (_holder != null) { _holder.ShowRequested -= value; } }
		}

		/// <summary>
		/// Whether caller instance is presumed to have started on sign in
		/// </summary>
		/// <returns>True if presumed to have started on sign in</returns>
		public bool IsStartedOnSignIn()
		{
			CheckWorker();
			return _worker.IsStartedOnSignIn();
		}

		/// <summary>
		/// Whether caller instance can be registered in startup
		/// </summary>
		/// <returns>True if can be registered</returns>
		public bool CanRegister()
		{
			CheckWorker();
			return _worker.CanRegister();
		}

		/// <summary>
		/// Whether caller instance is registered in startup
		/// </summary>
		/// <returns>True if already registered</returns>
		public bool IsRegistered()
		{
			CheckWorker();
			return _worker.IsRegistered();
		}

		/// <summary>
		/// Registers caller instance to startup.
		/// </summary>
		/// <returns>True if successfully registered</returns>
		public bool Register()
		{
			CheckWorker();
			return _worker.Register();
		}

		/// <summary>
		/// Unregisters caller instance from startup.
		/// </summary>
		public void Unregister()
		{
			CheckWorker();
			_worker.Unregister();
		}

		private void CheckWorker()
		{
			if (_worker == null)
				throw new InvalidOperationException("Functions have not started yet or failed to start.");
		}
	}
}