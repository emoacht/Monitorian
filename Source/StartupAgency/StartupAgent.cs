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
	public class StartupAgent
	{
		private readonly IStartupWorker _worker;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startupTaskId">Startup task ID</param>
		/// <param name="caller">Caller assembly</param>
		public StartupAgent(string startupTaskId, Assembly caller = null)
		{
			if (string.IsNullOrWhiteSpace(startupTaskId))
				throw new ArgumentNullException(nameof(startupTaskId));

			if (caller == null)
				caller = Assembly.GetCallingAssembly();

			_worker = (OsVersion.Is10Redstone1OrNewer && PlatformInfo.IsPackaged)
				? (IStartupWorker)new BridgeWorker(taskId: startupTaskId)
				: (IStartupWorker)new RegistryWorker(title: caller.GetTitle(), path: caller.Location);
		}

		/// <summary>
		/// Whether this instance is presumed to have started on sign in
		/// </summary>
		/// <returns>True if started on sign in</returns>
		public bool IsStartedOnSignIn() => _worker.IsStartedOnSignIn();

		/// <summary>
		/// Whether this instance can be registered in startup
		/// </summary>
		/// <returns>True if can be registered</returns>
		public bool CanRegister() => _worker.CanRegister();

		/// <summary>
		/// Whether this instance is registered in startup
		/// </summary>
		/// <returns>True if already registered</returns>
		public bool IsRegistered() => _worker.IsRegistered();

		/// <summary>
		/// Registers this instance to startup.
		/// </summary>
		/// <returns>True if successfully registered</returns>
		public bool Register() => _worker.Register();

		/// <summary>
		/// Unregisters this instance from startup.
		/// </summary>
		public void Unregister() => _worker.Unregister();
	}
}