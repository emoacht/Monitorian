using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

using StartupAgency.Bridge;
using StartupAgency.Helper;

namespace StartupAgency.Worker;

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

	public bool? IsStartedOnSignIn()
	{
		if (!IsRegistered())
			return false;

		if (OsVersion.Is10Build17134OrGreater)
			return IsActivatedByStartupTask();

		return null;
	}

	public bool CanRegister() => StartupTaskBroker.CanEnable(_taskId);

	public bool IsRegistered() => StartupTaskBroker.IsEnabled(_taskId);

	public bool Register() => StartupTaskBroker.Enable(_taskId);

	public void Unregister() => StartupTaskBroker.Disable(_taskId);

	private static bool? IsActivatedByStartupTask()
	{
		var args = AppInstance.GetActivatedEventArgs();
		if (args is null)
			return null;

		return (args.Kind == ActivationKind.StartupTask);
	}
}