﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Physical monitor controlled by DDC/CI (external monitor)
/// </summary>
internal class DdcMonitorItem : MonitorItem
{
	private readonly SafePhysicalMonitorHandle _handle;
	private readonly MonitorCapability _capability;

	public override bool IsBrightnessSupported => _capability.IsBrightnessSupported;
	public override bool IsContrastSupported => _capability.IsContrastSupported;
	public override bool IsPrecleared => _capability.IsPrecleared;

	public DdcMonitorItem(
		string deviceInstanceId,
		string description,
		byte displayIndex,
		byte monitorIndex,
		Rect monitorRect,
		SafePhysicalMonitorHandle handle,
		MonitorCapability capability) : base(
			deviceInstanceId: deviceInstanceId,
			description: description,
			displayIndex: displayIndex,
			monitorIndex: monitorIndex,
			monitorRect: monitorRect,
			isInternal: false,
			isReachable: true)
	{
		this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
		this._capability = capability ?? throw new ArgumentNullException(nameof(capability));
	}

	private uint _minimumBrightness = 0; // Raw minimum brightness (not always 0)
	private uint _maximumBrightness = 100; // Raw maximum brightness (not always 100)

	public override AccessResult UpdateBrightness(int value = -1)
	{
		var (result, minimum, current, maximum) = MonitorConfiguration.GetBrightness(_handle, _capability.IsHighLevelBrightnessSupported);

		if ((result.Status is AccessStatus.Succeeded) && (minimum < maximum) && (minimum <= current) && (current <= maximum))
		{
			this.Brightness = (int)Math.Round((double)(current - minimum) / (maximum - minimum) * 100D, MidpointRounding.AwayFromZero);
			this._minimumBrightness = minimum;
			this._maximumBrightness = maximum;
		}
		else
		{
			this.Brightness = -1; // Default
		}
		return result;
	}

	public override AccessResult SetBrightness(int brightness)
	{
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be from 0 to 100.");

		var buffer = (uint)Math.Round(brightness / 100D * (_maximumBrightness - _minimumBrightness) + _minimumBrightness, MidpointRounding.AwayFromZero);
		var result = MonitorConfiguration.SetBrightness(_handle, buffer, _capability.IsHighLevelBrightnessSupported);

		if (result.Status is AccessStatus.Succeeded)
		{
			this.Brightness = brightness;
		}
		return result;
	}

	private uint _minimumContrast = 0; // Raw minimum contrast (0)
	private uint _maximumContrast = 100; // Raw maximum contrast (not always 100)

	public override AccessResult UpdateContrast()
	{
		var (result, minimum, current, maximum) = MonitorConfiguration.GetContrast(_handle);

		if ((result.Status is AccessStatus.Succeeded) && (minimum < maximum) && (minimum <= current) && (current <= maximum))
		{
			this.Contrast = (int)Math.Round((double)(current - minimum) / (maximum - minimum) * 100D, MidpointRounding.AwayFromZero);
			this._minimumContrast = minimum;
			this._maximumContrast = maximum;
		}
		else
		{
			this.Contrast = -1; // Default
		}
		return result;
	}

	public override AccessResult SetContrast(int contrast)
	{
		if (contrast is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(contrast), contrast, "The contrast must be from 0 to 100.");

		var buffer = (uint)Math.Round(contrast / 100D * (_maximumContrast - _minimumContrast) + _minimumContrast, MidpointRounding.AwayFromZero);
		var result = MonitorConfiguration.SetContrast(_handle, buffer);

		if (result.Status is AccessStatus.Succeeded)
		{
			this.Contrast = contrast;
		}
		return result;
	}

	public override (AccessResult result, ValueData data) GetValue(byte code)
	{
		if ((_capability.CapabilitiesCodes is null)
			|| !_capability.CapabilitiesCodes.TryGetValue(code, out var values))
			return (AccessResult.NotSupported, null);

		var (result, _, current, _) = MonitorConfiguration.GetValue(_handle, code);

		if (result.Status is AccessStatus.Succeeded)
		{
			return (result, new ValueData((byte)current, values));
		}
		return (result, null);
	}

	public override (AccessResult result, ValueData data) SetValue(byte code, int value)
	{
		if ((_capability.CapabilitiesCodes is null)
			|| !_capability.CapabilitiesCodes.TryGetValue(code, out var values)
			|| (value is < byte.MinValue or > byte.MaxValue)
			|| (values?.Contains((byte)value) is false))
			return (AccessResult.NotSupported, null);

		var result = MonitorConfiguration.SetValue(_handle, code, (uint)value);

		if (result.Status is AccessStatus.Succeeded)
		{
			return (result, new ValueData((byte)value, values));
		}
		return (result, null);
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
			_handle.Dispose();
		}

		// Free any unmanaged objects here.
		_isDisposed = true;

		base.Dispose(disposing);
	}

	#endregion
}

internal class SafePhysicalMonitorHandle : SafeHandle
{
	public SafePhysicalMonitorHandle(IntPtr handle) : base(IntPtr.Zero, true)
	{
		this.handle = handle; // IntPtr.Zero may be a valid handle.
	}

	public override bool IsInvalid => false; // The validity cannot be checked by the handle.

	protected override bool ReleaseHandle()
	{
		return MonitorConfiguration.DestroyPhysicalMonitor(handle);
	}
}