using System;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Physical monitor controlled under High Dynamic Range (either internal or external monitor)
/// </summary>
internal class HdrMonitorItem : MonitorItem
{
	private readonly Luid _displayConfigId;

	public HdrMonitorItem(
		string deviceInstanceId,
		string description,
		byte displayIndex,
		byte monitorIndex,
		Rect monitorRect,
		bool isInternal,
		IntPtr monitorHandle,
		Luid displayConfigId) : base(
			deviceInstanceId,
			description,
			displayIndex,
			monitorIndex,
			monitorRect,
			isInternal,
			isReachable: true)
	{
		this._displayConfigId = displayConfigId ?? throw new ArgumentNullException(nameof(displayConfigId));

		DisplayInformationProvider.RegisterMonitor(DeviceInstanceId, monitorHandle);
	}

	private float _minimumBrightness = 80; // Raw minimum brightness (typically 80)
	private float _maximumBrightness = 480; // Raw maximum brightness (typically 480)

	public override AccessResult UpdateBrightness(int brightness = -1)
	{
		var (result, current, minimum, maximum) = DisplayInformationProvider.GetSdrWhiteLevel(DeviceInstanceId);

		if (result.Status is AccessStatus.Succeeded)
		{
			_minimumBrightness = Math.Min(_minimumBrightness, current);
			_maximumBrightness = Math.Max(_maximumBrightness, current);

			if (_minimumBrightness >= _maximumBrightness)
				return new AccessResult(AccessStatus.Failed, $"Current: {current}, Minimum: {_minimumBrightness}, Maximum: {_maximumBrightness}");

			float value = ((current - _minimumBrightness) / (_maximumBrightness - _minimumBrightness) * 100F);
			this.Brightness = (int)Math.Round(value, MidpointRounding.AwayFromZero);
		}
		return result;
	}

	public override AccessResult SetBrightness(int brightness)
	{
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be from 0 to 100.");

		float value = (brightness / 100F * (_maximumBrightness - _minimumBrightness) + _minimumBrightness);
		var result = DisplayConfig.SetSdrWhiteLevel(_displayConfigId, value);

		if (result.Status is AccessStatus.Succeeded)
		{
			this.Brightness = brightness;
		}
		return result;
	}

	#region IDisposable

	private bool _isDisposed = false;

	protected override void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			// Free any other managed objects here.
			DisplayInformationProvider.UnregisterMonitor(DeviceInstanceId);
		}

		// Free any unmanaged objects here.
		_isDisposed = true;

		base.Dispose(disposing);
	}

	#endregion
}