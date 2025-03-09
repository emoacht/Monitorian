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

	public BridgeWorker(string taskId)
	{
		if (string.IsNullOrWhiteSpace(taskId))
			throw new ArgumentNullException(nameof(taskId));

		this._taskId = taskId;
	}

	public bool? IsStartedOnSignIn()
	{
		if (!IsRegistered())
			return false;

		if (OsVersion.Is10Build17134OrGreater)
			return _isActivatedByStartupTask.Value;

		return null;
	}

	private readonly static Lazy<bool?> _isActivatedByStartupTask = new(() => IsActivatedByStartupTask());

	private static bool? IsActivatedByStartupTask()
	{
		try
		{
			// This method only returns arguments on its first call.
			// https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appinstance.getactivatedeventargs
			var args = AppInstance.GetActivatedEventArgs();
			if (args is null)
				return null;

			return (args.Kind == ActivationKind.StartupTask);
		}
		catch (Exception ex) when ((uint)ex.HResult is 0x800706BA)
		{
			// Error message: The RPC server is unavailable.
			// Error code: 0x800706BA = RPC_S_SERVER_UNAVAILABLE
			return null;
		}
	}

	public bool CanRegister() => StartupTaskBroker.CanEnable(_taskId);

	public bool IsRegistered() => StartupTaskBroker.IsEnabled(_taskId);

	public bool Register() => StartupTaskBroker.Enable(_taskId);

	public void Unregister() => StartupTaskBroker.Disable(_taskId);
}