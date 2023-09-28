using System;
using System.Collections.Generic;
using System.Linq;

using Monitorian.Core.Helper;
using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.Models.Watcher;

internal class DisplayInformationWatcher : IDisposable
{
	/// <summary>
	/// Options
	/// </summary>
	public static IReadOnlyCollection<string> Options => new[] { AdvancedColorOption };

	private const string AdvancedColorOption = "/advancedcolor";

	public static bool IsEnabled => _isEnabled.Value;
	private static readonly Lazy<bool> _isEnabled = new(() =>
	{
		return OsVersion.Is11Build22621OrGreater &&
			AppKeeper.StandardArguments.Select(x => x.ToLower()).Contains(AdvancedColorOption);
	});

	private Action<string, string> _onDisplayInformationChanged;

	public DisplayInformationWatcher()
	{
		if (!IsEnabled)
			return;

		DisplayInformationProvider.EnsureDispatcherQueue();
	}

	public void Subscribe(Action<string, string> onDisplayInformationChanged)
	{
		if (!IsEnabled)
			return;

		this._onDisplayInformationChanged = onDisplayInformationChanged ?? throw new ArgumentNullException(nameof(onDisplayInformationChanged));
		DisplayInformationProvider.AdvancedColorInfoChanged += OnAdvanctedColorInfoChanged;
	}

	private void OnAdvanctedColorInfoChanged(object sender, string e)
	{
		var colorInfo = ((Windows.Graphics.Display.DisplayInformation)sender).GetAdvancedColorInfo();
		_onDisplayInformationChanged?.Invoke(e, $"SDR WL: {colorInfo.SdrWhiteLevelInNits} Min: {colorInfo.MinLuminanceInNits:f1} Max: {colorInfo.MaxLuminanceInNits:f1} [{colorInfo.CurrentAdvancedColorKind}]");
	}

	public static Action RegisterMonitor(string deviceInstanceId, IntPtr monitorHandle)
	{
		if (!IsEnabled)
			return null;

		return DisplayInformationProvider.RegisterMonitor(deviceInstanceId, monitorHandle);
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
			DisplayInformationProvider.AdvancedColorInfoChanged -= OnAdvanctedColorInfoChanged;
			DisplayInformationProvider.ClearMonitors();
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}