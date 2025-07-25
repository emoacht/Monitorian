﻿using System;
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
		DisplayIdSet displayIdSet) : base(
			deviceInstanceId: deviceInstanceId,
			description: description,
			displayIndex: displayIndex,
			monitorIndex: monitorIndex,
			monitorRect: monitorRect,
			isInternal: isInternal,
			isReachable: true)
	{
		this._displayIdSet = displayIdSet ?? throw new ArgumentNullException(nameof(displayIdSet));

		DisplayInformationProvider.RegisterMonitor(DeviceInstanceId, monitorHandle);

		if (MonitorRecord.TryRead(DeviceInstanceId, out float minimum, out float maximum))
		{
			_minimumBrightness = minimum;
			_maximumBrightness = maximum;
		}
	}

	private float _minimumBrightness = 80F; // Raw minimum brightness (normally 80)
	private float _maximumBrightness = 0; // Raw maximum brightness

	public override AccessResult UpdateBrightness(int brightness = -1)
	{
		var (result, current, _, maximum) = DisplayInformationProvider.GetSdrWhiteLevel(DeviceInstanceId);

		if (result.Status is AccessStatus.Succeeded)
		{
			maximum = Math.Max(current, maximum);
			if ((current < _minimumBrightness) || (_maximumBrightness < maximum))
			{
				_minimumBrightness = Math.Min(_minimumBrightness, current);
				_maximumBrightness = Math.Max(_maximumBrightness, maximum);

				MonitorRecord.Write(DeviceInstanceId, _minimumBrightness, _maximumBrightness);
			}

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
		var result = DisplayConfig.SetSdrWhiteLevel(_displayIdSet, value);

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