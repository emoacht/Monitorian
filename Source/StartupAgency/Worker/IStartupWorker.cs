using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartupAgency.Worker
{
	internal interface IStartupWorker
	{
		/// <summary>
		/// Whether this instance is started on sign in
		/// </summary>
		bool IsStartedOnSignIn();

		/// <summary>
		/// Whether this instance can be registered in startup
		/// </summary>
		bool CanRegister();

		/// <summary>
		/// Whether this instance is registered in startup
		/// </summary>
		bool IsRegistered();

		/// <summary>
		/// Registers this instance to startup.
		/// </summary>
		bool Register();

		/// <summary>
		/// Unregisters this instance from startup.
		/// </summary>
		void Unregister();
	}
}