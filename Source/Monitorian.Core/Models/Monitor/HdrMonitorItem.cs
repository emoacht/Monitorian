using System;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Physical monitor controlled under High Dynamic Range (either internal or external monitor)
/// </summary>
internal class HdrMonitorItem : MonitorItem
{
	private readonly DisplayIdSet _displayIdSet;

	public HdrMonitorItem(
		string deviceInstanceId,
		string description,
		byte displayIndex,
		byte monitorIndex,
		Rect monitorRect,
		bool isInternal,
		IntPtr monitorHandle,
		DisplayIdSet displayIdSet,
		int sdrWhiteLevel = -1) : base(
			deviceInstanceId: deviceInstanceId,
			description: description,
			displayIndex: displayIndex,
			monitorIndex: monitorIndex,
			monitorRect: monitorRect,
			isInternal: isInternal,
			isReachable: true)
	{
		this._displayIdSet = displayIdSet ?? throw new ArgumentNullException(nameof(displayIdSet));

		if (0 <= sdrWhiteLevel)
			UpdateBrightness(sdrWhiteLevel);

		DisplayInformationProvider.RegisterMonitor(DeviceInstanceId, monitorHandle);
	}

	private const float MinimumWhiteLevel = 80F; // Raw minimum white level (always 80 nits)
	private const float MaximumWhiteLevel = 480F; // Raw maximum white level (always 480 nits)

	public override AccessResult UpdateBrightness(int sdrWhiteLevel = -1)
	{
		float buffer = sdrWhiteLevel;
		if (buffer < 0)
		{
			(var result, buffer) = DisplayInformationProvider.GetSdrWhiteLevel(DeviceInstanceId);
			if (result.Status is not AccessStatus.Succeeded)
				return result;
		}

		float brightness = ((buffer - MinimumWhiteLevel) / (MaximumWhiteLevel - MinimumWhiteLevel) * 100F);
		this.Brightness = (int)Math.Round(brightness, MidpointRounding.AwayFromZero);
		return AccessResult.Succeeded;
	}

	public override AccessResult SetBrightness(int brightness)
	{
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be from 0 to 100.");

		float sdrWhiteLevel = (brightness / 100F * (MaximumWhiteLevel - MinimumWhiteLevel) + MinimumWhiteLevel);
		var result = DisplayConfig.SetSdrWhiteLevel(_displayIdSet, sdrWhiteLevel);

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