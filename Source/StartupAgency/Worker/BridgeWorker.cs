using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

using StartupAgency.Bridge;

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
		/// <remarks>Startup task ID must match that in AppxManifest.xml.</remarks>
		private readonly string _taskId;

		private DateTimeOffset _lastStartTime;

		public BridgeWorker(string taskId)
		{
			if (string.IsNullOrWhiteSpace(taskId))
				throw new ArgumentNullException(nameof(taskId));

			this._taskId = taskId;

			// Get and update last start time.
			_lastStartTime = StartupData.LastStartTime;
		}

		public bool IsStartedOnSignIn()
		{
			if (!IsRegistered())
				return false;

			// Compare last start time with session start time.
			if (TryGetLogonSessionStartTime(out DateTimeOffset sessionStartTime) &&
				(sessionStartTime < _lastStartTime))
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

			try
			{
				using (var searcher = new ManagementObjectSearcher(query))
				using (var sessions = searcher.Get())
				{
					foreach (ManagementObject session in sessions)
					{
						using (session)
						{
							var startTimeString = (string)session.GetPropertyValue("StartTime");
							if (string.IsNullOrEmpty(startTimeString))
								continue;

							startTime = new DateTimeOffset(ManagementDateTimeConverter.ToDateTime(startTimeString));
							return true;
						}
					}
				}
			}
			catch (ManagementException me)
			{
				Debug.WriteLine($"Failed to get logon session start time. HResult: {me.HResult} ErrorCode: {me.ErrorCode}" + Environment.NewLine
					+ me);
			}

			startTime = default;
			return false;
		}

		#endregion
	}
}