using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

using StartupBridge;

namespace StartupAgency.Worker
{
	/// <summary>
	/// Startup task (AppX) worker
	/// </summary>
	internal class BridgeWorker : IStartupWorker
	{
		/// <summary>
		/// Startup task ID
		/// </summary>
		/// <remarks>This ID must match that in AppxManifest.xml.</remarks>
		private readonly string _taskId;

		public BridgeWorker(string taskId)
		{
			if (string.IsNullOrWhiteSpace(taskId))
				throw new ArgumentNullException(nameof(taskId));

			this._taskId = taskId;
		}

		public bool IsStartedOnSignIn()
		{
			// Get and update last start time.
			var lastStartTime = StartupData.LastStartTime;

			if (!IsRegistered())
				return false;

			// Compare last start time with session start time.
			if (TryGetLogonSessionStartTime(out DateTimeOffset sessionStartTime) &&
				(sessionStartTime < lastStartTime))
				return false;

			return true;
		}

		public bool CanRegister() => StartupTaskBroker.CanEnable(_taskId);

		public bool IsRegistered() => StartupTaskBroker.IsEnabled(_taskId);

		public bool Register() => StartupTaskBroker.Enable(_taskId);

		public void Unregister() => StartupTaskBroker.Disable(_taskId);

		#region Session

		private static bool TryGetLogonSessionStartTime(out DateTimeOffset startTime)
		{
			var query = new SelectQuery("Win32_LogonSession", "LogonType = 2");
			using (var searcher = new ManagementObjectSearcher(query))
			using (var sessions = searcher.Get())
			{
				foreach (ManagementObject session in sessions)
				{
					using (session)
					{
						var buff = (string)session.GetPropertyValue("StartTime");
						if (string.IsNullOrEmpty(buff))
							continue;

						startTime = new DateTimeOffset(ManagementDateTimeConverter.ToDateTime(buff));
						return true;
					}
				}
			}
			startTime = default(DateTimeOffset);
			return false;
		}

		#endregion
	}
}