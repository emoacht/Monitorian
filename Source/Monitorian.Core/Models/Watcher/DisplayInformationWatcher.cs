using System;

using Monitorian.Core.Helper;
using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.Models.Watcher;

internal class DisplayInformationWatcher : IDisposable
{
	private Action<string, float> _onDisplayInformationChanged;

	public DisplayInformationWatcher()
	{ }

	public void Subscribe(Action<string, float> onDisplayInformationChanged)
	{
		this._onDisplayInformationChanged = onDisplayInformationChanged ?? throw new ArgumentNullException(nameof(onDisplayInformationChanged));
	}

	private void OnAdvancedColorInfoChanged(object sender, (string deviceInstanceId, float sdrWhiteLevel) e)
	{
		_onDisplayInformationChanged?.Invoke(e.deviceInstanceId, e.sdrWhiteLevel);
	}

	public static bool IsEnabled { get; private set; }

	public bool TryEnable()
	{
		if (!OsVersion.Is11Build22621OrGreater || IsEnabled)
			return false;

		DisplayInformationProvider.EnsureDispatcherQueue();
		DisplayInformationProvider.AdvancedColorInfoChanged += OnAdvancedColorInfoChanged;
		IsEnabled = true;
		return true;
	}

	public void Disable()
	{
		if (!IsEnabled)
			return;

		DisplayInformationProvider.AdvancedColorInfoChanged -= OnAdvancedColorInfoChanged;
		IsEnabled = false;
	}

	#region IDisposable

	private bool _isDisposed = false;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			// Free any other managed objects here.
			DisplayInformationProvider.AdvancedColorInfoChanged -= OnAdvancedColorInfoChanged;
			DisplayInformationProvider.ClearMonitors();
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}