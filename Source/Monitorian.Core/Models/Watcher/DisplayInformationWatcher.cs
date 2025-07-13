using System;

using Monitorian.Core.Helper;
using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.Models.Watcher;

internal class DisplayInformationWatcher : IDisposable
{
	private Action<string, string> _onDisplayInformationChanged;

	public DisplayInformationWatcher()
	{ }

	public void Subscribe(Action<string, string> onDisplayInformationChanged)
	{
		this._onDisplayInformationChanged = onDisplayInformationChanged ?? throw new ArgumentNullException(nameof(onDisplayInformationChanged));
	}

	private void OnAdvancedColorInfoChanged(object sender, string e)
	{
		var colorInfo = ((Windows.Graphics.Display.DisplayInformation)sender).GetAdvancedColorInfo();
		_onDisplayInformationChanged?.Invoke(e, $"SDR WL: {colorInfo.SdrWhiteLevelInNits} Min: {colorInfo.MinLuminanceInNits:f1} Max: {colorInfo.MaxLuminanceInNits:f1} [{colorInfo.CurrentAdvancedColorKind}]");
	}

	public static bool IsEnabled { get; private set; } = false;

	public void TryEnable()
	{
		if (!OsVersion.Is11Build22621OrGreater || IsEnabled)
			return;

		DisplayInformationProvider.EnsureDispatcherQueue();
		DisplayInformationProvider.AdvancedColorInfoChanged += OnAdvancedColorInfoChanged;
		IsEnabled = true;
	}

	public void Disable()
	{
		if (!OsVersion.Is11Build22621OrGreater || !IsEnabled)
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