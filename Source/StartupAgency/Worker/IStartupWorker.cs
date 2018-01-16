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
		/// Whether caller instance is presumed to have started on sign in
		/// </summary>
		bool IsStartedOnSignIn();

		/// <summary>
		/// Whether caller instance can be registered in startup
		/// </summary>
		bool CanRegister();

		/// <summary>
		/// Whether caller instance is registered in startup
		/// </summary>
		bool IsRegistered();

		/// <summary>
		/// Registers caller instance to startup.
		/// </summary>
		bool Register();

		/// <summary>
		/// Unregisters caller instance from startup.
		/// </summary>
		void Unregister();
	}
}