using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace StartupBridge
{
	public static class StartupTaskBroker
	{
		/// <summary>
		/// Whether the startup task for a specified AppX package can be enabled
		/// </summary>
		/// <param name="taskId">Id of startup task</param>
		/// <returns>True if the startup task can be enabled</returns>
		public static bool CanEnable(string taskId)
		{
			if (!PlatformInfo.IsPackaged)
				return false;

			var task = GetStartupTask(taskId);
			return (task.State != StartupTaskState.DisabledByUser);
		}

		/// <summary>
		/// Whether the startup task for a specified AppX package is enabled
		/// </summary>
		/// <param name="taskId">Id of startup task</param>
		/// <returns>True if the startup task is enabled</returns>
		public static bool IsEnabled(string taskId)
		{
			if (!PlatformInfo.IsPackaged)
				return false;

			var task = GetStartupTask(taskId);
			return (task.State == StartupTaskState.Enabled);
		}

		/// <summary>
		/// Enables the startup task for a specified AppX package.
		/// </summary>
		/// <param name="taskId">Id of startup task</param>
		/// <returns>True if the startup task is enabled</returns>
		public static bool Enable(string taskId)
		{
			if (!PlatformInfo.IsPackaged)
				return false;

			var task = GetStartupTask(taskId);
			switch (task.State)
			{
				case StartupTaskState.Enabled:
					return true;

				case StartupTaskState.Disabled:
					var result = task.RequestEnableAsync().AsTask().Result;
					return (result == StartupTaskState.Enabled);

				default:
					return false;
			}
		}

		/// <summary>
		/// Disables the startup task for a specified AppX package.
		/// </summary>
		/// <param name="taskId">Id of startup task</param>
		public static void Disable(string taskId)
		{
			if (!PlatformInfo.IsPackaged)
				return;

			var task = GetStartupTask(taskId);
			switch (task.State)
			{
				case StartupTaskState.Enabled:
					task.Disable();
					break;
			}
		}

		private static StartupTask GetStartupTask(string taskId)
		{
			if (string.IsNullOrWhiteSpace(taskId))
				throw new ArgumentNullException(nameof(taskId));

			return StartupTask.GetAsync(taskId).AsTask().Result;
		}
	}
}