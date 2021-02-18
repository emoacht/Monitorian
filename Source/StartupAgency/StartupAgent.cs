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
		private PipeHolder _holder;

		/// <summary>
		/// Starts.
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="startupTaskId">Startup task ID</param>
		/// <param name="args">Arguments to another instance</param>
		/// <returns>
		/// <para>success: True if no other instance exists and this instance successfully starts</para>
		/// <para>response: Response from another instance if that instance exists and returns an response</para> 
		/// </returns>
		/// <remarks>Startup task ID must match that in AppxManifest.xml.</remarks>
		public (bool success, string response) Start(string name, string startupTaskId, IReadOnlyList<string> args)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));
			if (string.IsNullOrWhiteSpace(startupTaskId))
				throw new ArgumentNullException(nameof(startupTaskId));

			_holder = new PipeHolder(name, null);
			var (success, response) = _holder.Create(args?.ToArray());
			if (!success)
				return (success: false, response);

			_worker = (OsVersion.Is10Redstone1OrNewer && IsPackaged)
				? (IStartupWorker)new BridgeWorker(taskId: startupTaskId)
				: (IStartupWorker)new RegistryWorker(name: name, path: Assembly.GetEntryAssembly().Location);
			return (success: true, null);
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
		/// Whether this assembly is packaged in AppX package
		/// </summary>
		public bool IsPackaged => PlatformInfo.IsPackaged;

		/// <summary>
		/// Delegate to asynchronously handle the arguments from another instance
		/// </summary>
		/// <remarks>
		/// This delegate will be called by a thread other than that instantiated this object.
		/// Accordingly, the appropriate use of dispatcher is required.
		/// </remarks>
		public Func<string[], Task<string>> HandleRequestAsync
		{
			get => _holder?.HandleRequestAsync;
			set { if (_holder is not null) { _holder.HandleRequestAsync = value; } }
		}

		private const string HideOption = "/hide";

		/// <summary>
		/// Options
		/// </summary>
		public static IReadOnlyCollection<string> Options => new[] { HideOption };

		/// <summary>
		/// Determines whether caller instance is expected to show its window.
		/// </summary>
		/// <returns>True if expected to be show its window</returns>
		public bool IsWindowShowExpected()
		{
			return !IsStartedOnSignIn()
				&& !Environment.GetCommandLineArgs().Skip(1).Contains(HideOption);
		}

		#region Register/Unregister

		/// <summary>
		/// Determines whether caller instance is presumed to have started on sign in.
		/// </summary>
		/// <returns>True if presumed to have started on sign in</returns>
		public bool IsStartedOnSignIn()
		{
			CheckWorker();
			return _worker.IsStartedOnSignIn();
		}

		/// <summary>
		/// Determines whether caller instance can be registered in startup.
		/// </summary>
		/// <returns>True if can be registered</returns>
		public bool CanRegister()
		{
			CheckWorker();
			return _worker.CanRegister();
		}

		/// <summary>
		/// Determines whether caller instance has been registered in startup.
		/// </summary>
		/// <returns>True if has been already registered</returns>
		public bool IsRegistered()
		{
			CheckWorker();
			return _worker.IsRegistered();
		}

		/// <summary>
		/// Registers caller instance to startup.
		/// </summary>
		/// <returns>True if successfully registers</returns>
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
			if (_worker is null)
				throw new InvalidOperationException("The functions have not started yet or have failed to start.");
		}

		#endregion
	}
}