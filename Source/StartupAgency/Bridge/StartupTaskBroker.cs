using System;
using Windows.ApplicationModel;

namespace StartupAgency.Bridge;

/// <summary>
/// Startup task broker
/// </summary>
/// <remarks>
/// This class wraps <see cref="Windows.ApplicationModel.StartupTask"/> which is only available
/// on Windows 10 (version 10.0.14393.0) or greater.
/// </remarks>
public static class StartupTaskBroker
{
	/// <summary>
	/// Determines whether the startup task for a specified AppX package can be enabled.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	/// <returns>True if the startup task can be enabled</returns>
	public static bool CanEnable(string taskId)
	{
		try
		{
			if (!PlatformInfo.IsPackaged)
				return false;

			var task = GetStartupTask(taskId);
			return (task.State is not StartupTaskState.DisabledByUser);
		}
		catch (Exception ex) when (ex.HResult == unchecked((int)0x800706be) || ex.HResult == unchecked((int)0x80131500))
		{
			System.Diagnostics.Debug.WriteLine("RPC failed during wake state in CanEnable. Defaulting to false.");
			return false;
		}
	}

	/// <summary>
	/// Determines whether the startup task for a specified AppX package has been enabled.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	/// <returns>True if the startup task has been enabled</returns>
	public static bool IsEnabled(string taskId)
	{
		try
		{
			if (!PlatformInfo.IsPackaged)
				return false;

			var task = GetStartupTask(taskId);
			return task.State == Windows.ApplicationModel.StartupTaskState.Enabled;
		}
		catch (Exception ex) when (ex.HResult == unchecked((int)0x800706be) || ex.HResult == unchecked((int)0x80131500))
		{
			// Swallow the RPC/COM exception during wake and fail gracefully
			System.Diagnostics.Debug.WriteLine("RPC failed during wake state. Defaulting to false.");
			return false;
		}
	}

	/// <summary>
	/// Enables the startup task for a specified AppX package.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	/// <returns>True if the startup task is enabled</returns>
	public static bool Enable(string taskId)
	{
		try
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
					return (result is StartupTaskState.Enabled);

				default:
					return false;
			}
		}
		catch (Exception ex) when (ex.HResult == unchecked((int)0x800706be) || ex.HResult == unchecked((int)0x80131500))
		{
			System.Diagnostics.Debug.WriteLine("RPC failed during wake state in Enable. Defaulting to false.");
			return false;
		}
	}

	/// <summary>
	/// Disables the startup task for a specified AppX package.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	public static void Disable(string taskId)
	{
		try
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
		catch (Exception ex) when (ex.HResult == unchecked((int)0x800706be) || ex.HResult == unchecked((int)0x80131500))
		{
			System.Diagnostics.Debug.WriteLine("RPC failed during wake state in Disable. Aborting cleanly.");
		}
	}

	private static StartupTask GetStartupTask(string taskId)
	{
		if (string.IsNullOrWhiteSpace(taskId))
			throw new ArgumentNullException(nameof(taskId));

		return StartupTask.GetAsync(taskId).AsTask().Result;
	}
}