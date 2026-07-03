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
		if (!PlatformInfo.IsPackaged)
			return false;

		if (!TryGetStartupTask(taskId, out var task))
			return false;

		return (task.State is not StartupTaskState.DisabledByUser);
	}

	/// <summary>
	/// Determines whether the startup task for a specified AppX package has been enabled.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	/// <returns>True if the startup task has been enabled</returns>
	public static bool? IsEnabled(string taskId)
	{
		if (!PlatformInfo.IsPackaged)
			return false;

		if (!TryGetStartupTask(taskId, out var task))
			return null;

		return (task.State is StartupTaskState.Enabled);
	}

	/// <summary>
	/// Enables the startup task for a specified AppX package.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	/// <returns>True if the startup task is enabled</returns>
	public static bool Enable(string taskId)
	{
		if (!PlatformInfo.IsPackaged)
			return false;

		if (!TryGetStartupTask(taskId, out var task))
			return false;

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

	/// <summary>
	/// Disables the startup task for a specified AppX package.
	/// </summary>
	/// <param name="taskId">Startup task ID</param>
	public static void Disable(string taskId)
	{
		if (!PlatformInfo.IsPackaged)
			return;

		if (!TryGetStartupTask(taskId, out var task))
			return;

		switch (task.State)
		{
			case StartupTaskState.Enabled:
				task.Disable();
				break;
		}
	}

	private static bool TryGetStartupTask(string taskId, out StartupTask task)
	{
		if (string.IsNullOrWhiteSpace(taskId))
			throw new ArgumentNullException(nameof(taskId));

		try
		{
			task = StartupTask.GetAsync(taskId).AsTask().Result;
			return true;
		}
		catch (Exception ex) when ((uint)ex.HResult is 0x800706BE)
		{
			// Error message: The remote procedure call failed.
			// Error code: 0x06BA = RPC_S_CALL_FAILED
			task = null;
			return false;
		}
	}
}