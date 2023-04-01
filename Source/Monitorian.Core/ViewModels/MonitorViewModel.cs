﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Monitorian.Core.Common;
using Monitorian.Core.Helper;
using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Properties;

namespace Monitorian.Core.ViewModels
{
	public class MonitorViewModel : ViewModelBase
	{
		private readonly AppControllerCore _controller;
		public SettingsCore Settings => _controller.Settings;

		private IMonitor _monitor;

		public MonitorViewModel(AppControllerCore controller, IMonitor monitor)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
			this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
			ToggleSpeakerMuteCommand = new RelayCommand(() => ToggleSpeakerMute(), () => IsSpeakerMuteSupported);
			SetTopLeft();

			LoadCustomization();
		}

		private readonly object _lock = new();

		internal void Replace(IMonitor monitor)
		{
			if (monitor is { IsReachable: true })
			{
				lock (_lock)
				{
					this._monitor.Dispose();
					this._monitor = monitor;
					SetTopLeft();
				}
			}
			else
			{
				monitor?.Dispose();
			}
		}

		public string DeviceInstanceId => _monitor.DeviceInstanceId;
		public string Description => _monitor.Description;
		public byte DisplayIndex => _monitor.DisplayIndex;
		public byte MonitorIndex => _monitor.MonitorIndex;
		public Rect MonitorRect => _monitor.MonitorRect;

		#region Customization

		private void LoadCustomization() => _controller.TryLoadCustomization(DeviceInstanceId, ref _name, ref _isUnison, ref _rangeLowest, ref _rangeHighest);
		private void SaveCustomization() => _controller.SaveCustomization(DeviceInstanceId, _name, _isUnison, _rangeLowest, _rangeHighest);

		public string Name
		{
			get => _name ?? _monitor.Description;
			set
			{
				if (SetProperty(ref _name, GetValueOrNull(value)))
					SaveCustomization();
			}
		}
		private string _name;

		private static string GetValueOrNull(string value) => !string.IsNullOrWhiteSpace(value) ? value : null;

		public bool IsUnison
		{
			get => _isUnison;
			set
			{
				if (SetProperty(ref _isUnison, value))
					SaveCustomization();
			}
		}
		private bool _isUnison;

		/// <summary>
		/// Whether the range of brightness is changing
		/// </summary>
		public bool IsRangeChanging
		{
			get => _isRangeChanging;
			set => SetProperty(ref _isRangeChanging, value);
		}
		private bool _isRangeChanging = false;

		/// <summary>
		/// Lowest brightness in the range of brightness
		/// </summary>
		public int RangeLowest
		{
			get => _rangeLowest;
			set
			{
				if (SetProperty(ref _rangeLowest, (byte)value))
					SaveCustomization();
			}
		}
		private byte _rangeLowest = 0;

		/// <summary>
		/// Highest brightness in the range of brightness
		/// </summary>
		public int RangeHighest
		{
			get => _rangeHighest;
			set
			{
				if (SetProperty(ref _rangeHighest, (byte)value))
					SaveCustomization();
			}
		}
		private byte _rangeHighest = 100;

		private double GetRangeRate() => Math.Abs(RangeHighest - RangeLowest) / 100D;

		#endregion

		#region Brightness

		public int Brightness
		{
			get => _monitor.Brightness;
			set
			{
				if (_monitor.Brightness == value)
					return;

				SetBrightness(value, false);

				if (IsSelected)
					_controller.SaveMonitorUserChanged(this);
			}
		}

		public int BrightnessSystemAdjusted => _monitor.BrightnessSystemAdjusted;
		public int BrightnessSystemChanged => Brightness;

		public bool UpdateBrightness(int brightness = -1)
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.UpdateBrightness(brightness);
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					BrightnessUpdatedTime = DateTimeOffset.Now;
					OnPropertyChanged(nameof(BrightnessSystemChanged)); // This must be prior to Brightness.
					OnPropertyChanged(nameof(Brightness));
					OnPropertyChanged(nameof(BrightnessSystemAdjusted));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		public bool? UpdateBrightnessIfElapsed(TimeSpan duration)
		{
			if (DateTimeOffset.Now - BrightnessUpdatedTime < duration)
				return null;

			return UpdateBrightness();
		}

		public void IncrementBrightness()
		{
			IncrementBrightness(10);

			if (IsSelected)
				_controller.SaveMonitorUserChanged(this);
		}

		public void IncrementBrightness(int tickSize, bool isCycle = true)
		{
			if (IsRangeChanging)
				return;

			var size = tickSize * GetRangeRate();
			var count = Math.Floor((Brightness - RangeLowest) / size + 0.01);
			int brightness = RangeLowest + (int)Math.Ceiling((count + 1) * size);

			SetBrightness(brightness, isCycle);
		}

		public void DecrementBrightness()
		{
			DecrementBrightness(10);

			if (IsSelected)
				_controller.SaveMonitorUserChanged(this);
		}

		public void DecrementBrightness(int tickSize, bool isCycle = true)
		{
			if (IsRangeChanging)
				return;

			var size = tickSize * GetRangeRate();
			var count = Math.Ceiling((Brightness - RangeLowest) / size - 0.01);
			int brightness = RangeLowest + (int)Math.Floor((count - 1) * size);

			SetBrightness(brightness, isCycle);
		}

		public void SetBrightness(int brightness) => SetBrightness(brightness, false);

		private bool SetBrightness(int brightness, bool isCycle)
		{
			if (brightness < RangeLowest)
				brightness = isCycle ? RangeHighest : RangeLowest;
			else if (RangeHighest < brightness)
				brightness = isCycle ? RangeLowest : RangeHighest;

			AccessResult result;
			lock (_lock)
			{
				result = _monitor.SetBrightness(brightness);
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					BrightnessUpdatedTime = DateTimeOffset.Now;
					OnPropertyChanged(nameof(Brightness));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.DdcFailed:
						case AccessStatus.TransmissionFailed:
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		public DateTimeOffset BrightnessUpdatedTime { get; private set; }

		#endregion

		#region Contrast

		public bool IsContrastSupported => _monitor.IsContrastSupported;

		public bool IsContrastChanging
		{
			get => IsContrastSupported && _isContrastChanging;
			set
			{
				if (SetProperty(ref _isContrastChanging, value) && value)
					UpdateContrast();
			}
		}
		private bool _isContrastChanging = false;

		public int Contrast
		{
			get => _monitor.Contrast;
			set
			{
				if (_monitor.Contrast == value)
					return;

				SetContrast(value);

				if (IsSelected)
					_controller.SaveMonitorUserChanged(this);
			}
		}

		public bool UpdateContrast()
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.UpdateContrast();
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					ContrastUpdatedTime = DateTimeOffset.Now;
					OnPropertyChanged(nameof(Contrast));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		public bool? UpdateContrastIfElapsed(TimeSpan duration)
		{
			if (DateTimeOffset.Now - ContrastUpdatedTime < duration)
				return null;

			return UpdateContrast();
		}

		public void IncrementContrast(int tickSize)
		{
			var size = (double)tickSize;
			var count = Math.Floor(Contrast / size);
			int contrast = (int)Math.Ceiling((count + 1) * size);

			SetContrast(contrast);
		}

		public void DecrementContrast(int tickSize)
		{
			var size = (double)tickSize;
			var count = Math.Ceiling(Contrast / size);
			int contrast = (int)Math.Floor((count - 1) * size);

			SetContrast(contrast);
		}

		private bool SetContrast(int contrast)
		{
			contrast = Math.Min(100, Math.Max(0, contrast));

			AccessResult result;
			lock (_lock)
			{
				result = _monitor.SetContrast(contrast);
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					ContrastUpdatedTime = DateTimeOffset.Now;
					OnPropertyChanged(nameof(Contrast));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.DdcFailed:
						case AccessStatus.TransmissionFailed:
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		public DateTimeOffset ContrastUpdatedTime { get; private set; }

		#endregion

		#region Temperature

		public bool IsTemperatureSupported => _monitor.IsTemperatureSupported;

		public void ChangeTemperature()
		{
			if (IsTemperatureSupported)
				_monitor.ChangeTemperature();
		}

		#endregion

		#region Speaker Volume

		public bool IsSpeakerVolumeSupported => _monitor.IsSpeakerVolumeSupported;		

		public int SpeakerVolume
		{
			get => _monitor.SpeakerVolume;
			set
			{
				if (_monitor.SpeakerVolume == value)
					return;

				SetSpeakerVolume(value);

				if (IsSelected)
					_controller.SaveMonitorUserChanged(this);
			}
		}

		public bool UpdateSpeakerVolume()
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.UpdateSpeakerVolume();
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					OnPropertyChanged(nameof(SpeakerVolume));
					OnPropertyChanged(nameof(IsSpeakerMute));
					OnPropertyChanged(nameof(SpeakerSymbol));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		private bool SetSpeakerVolume(int contrast)
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.SetSpeakerVolume(contrast);
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					OnPropertyChanged(nameof(SpeakerVolume));
					OnPropertyChanged(nameof(IsSpeakerMute));
					OnPropertyChanged(nameof(SpeakerSymbol));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.DdcFailed:
						case AccessStatus.TransmissionFailed:
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		#endregion

		#region Speaker Mute
		public bool IsSpeakerMuteSupported => _monitor.IsSpeakerMuteSupported;
		public ICommand ToggleSpeakerMuteCommand { get; set; }

		public string SpeakerSymbol => IsSpeakerMute ? "\xE74F" : "\xE767";

		public bool IsSpeakerMute
		{
			get => _monitor.IsSpeakerMute;
			set
			{
				if (_monitor.IsSpeakerMute == value)
					return;

				ToggleSpeakerMute();

				if (IsSelected)
					_controller.SaveMonitorUserChanged(this);
			}
		}

		public bool UpdateIsSpeakerMute()
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.UpdateIsSpeakerMute();
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					OnPropertyChanged(nameof(IsSpeakerMute));
					OnPropertyChanged(nameof(SpeakerSymbol));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		private bool ToggleSpeakerMute()
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.ToggleSpeakerMute();
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					OnPropertyChanged(nameof(IsSpeakerMute));
					OnPropertyChanged(nameof(SpeakerSymbol));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.DdcFailed:
						case AccessStatus.TransmissionFailed:
						case AccessStatus.NoLongerExist:
							_controller.OnMonitorsChangeFound();
							break;
					}
					OnFailed();
					return false;
			}
		}

		#endregion

		#region Controllable

		public bool IsReachable => _monitor.IsReachable;

		public bool IsControllable => IsReachable && ((0 < _controllableCount) || _isConfirmed);
		private bool _isConfirmed;

		// This count is for determining IsControllable property.
		// To set this count, the following points need to be taken into account: 
		// - The initial value of IsControllable property should be true (provided IsReachable is
		//   true) because a monitor is expected to be controllable. Therefore, the initial count
		//   should be greater than 0.
		// - The initial count is intended to give allowance for failures before the first success.
		//   If the count has been consumed without any success, the monitor will be regarded as
		//   uncontrollable at all.
		// - _isConfirmed field indicates that the monitor has succeeded at least once. It will be
		//   set true at the first success and at a succeeding success after a failure.
		// - The normal count gives allowance for failures after the first and succeeding successes.
		//   As long as the monitor continues to succeed, the count will stay at the normal count.
		//   Each time the monitor fails, the count decreases. The decreased count will be reverted
		//   to the normal count when the monitor succeeds again.
		// - The initial count must be smaller than the normal count so that _isConfirmed field
		//   will be set at the first success while reducing unnecessary access to the field.
		// - If an unreachable monitor is found and added to monitors collection and if there is no
		//   controllable monitor, the unreachable monitor will be made to target (IsTarget
		//   property will be true) to make it appear in view. In such case, the initial value of
		//   IsControllable property will be false because IsReachable property is false while
		//   Message property remains null until this count decreases to 0. Since IsReachable
		//   property is false, scan process will not change this count but update process will do.
		//   If such monitor is turned to be reachable and succeeds, this count will be normal count
		//   and IsControllable property will be true. To notify this change to view, this count
		//   (copied to former count) will have to pass value check inside OnSucceeded method.
		//   If a monitor is first found unreachable but immediately turned to be reachable before
		//   this count decreases, this count will remain as initial count. For this reason,
		//   the value to be compared with this count must not be smaller than initial count.
		private short _controllableCount = InitialCount;
		private const short InitialCount = 3;
		private const short NormalCount = 5;

		private void OnSucceeded()
		{
			if (_controllableCount < NormalCount)
			{
				var formerCount = _controllableCount;
				_controllableCount = NormalCount;
				if (formerCount <= InitialCount)
				{
					OnPropertyChanged(nameof(IsControllable));
					OnPropertyChanged(nameof(Message));
				}

				_isConfirmed = true;
			}
		}

		private void OnFailed()
		{
			if (--_controllableCount == 0)
			{
				OnPropertyChanged(nameof(IsControllable));
				OnPropertyChanged(nameof(Message));
			}
		}

		public string Message
		{
			get
			{
				if (IsReachable && (0 < _controllableCount))
					return null;

				LanguageService.Switch();

				var reason = _monitor switch
				{
					DdcMonitorItem => Resources.StatusReasonDdcFailing,
					UnreachableMonitorItem { IsInternal: false } => Resources.StatusReasonDdcNotEnabled,
					_ => null,
				};

				return Resources.StatusNotControllable + (reason is null ? string.Empty : Environment.NewLine + reason);
			}
		}

		#endregion

		#region Focus

		public bool IsByKey
		{
			get => _isByKey;
			set
			{
				if (SetProperty(ref _isByKey, value))
					OnPropertyChanged(nameof(IsSelectedByKey));
			}
		}
		private bool _isByKey;

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (SetProperty(ref _isSelected, value))
					OnPropertyChanged(nameof(IsSelectedByKey));
			}
		}
		private bool _isSelected;

		public bool IsSelectedByKey => IsSelected && IsByKey;

		#endregion

		#region Arrangement

		public ulong MonitorTopLeft
		{
			get => _monitorTopLeft;
			private set => SetProperty(ref _monitorTopLeft, value);
		}
		private ulong _monitorTopLeft;

		private void SetTopLeft()
		{
			MonitorTopLeft = GetTopLeft(_monitor.MonitorRect.Location);

			static ulong GetTopLeft(Point location)
			{
				var x = (long)Math.Round(location.X, MidpointRounding.AwayFromZero) + int.MaxValue;
				var y = (long)Math.Round(location.Y, MidpointRounding.AwayFromZero) + int.MaxValue;
				return (ulong)x | ((ulong)y << 32);
			}
		}

		#endregion

		public bool IsTarget
		{
			get => _isTarget;
			set => SetProperty(ref _isTarget, value);
		}
		private bool _isTarget;

		public override string ToString()
		{
			return SimpleSerialization.Serialize(
				("Item", _monitor),
				(nameof(Name), Name),
				(nameof(IsUnison), IsUnison),
				(nameof(IsControllable), IsControllable),
				("IsConfirmed", _isConfirmed),
				("ControllableCount", _controllableCount),
				(nameof(IsByKey), IsByKey),
				(nameof(IsSelected), IsSelected),
				(nameof(IsTarget), IsTarget));
		}

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			lock (_lock)
			{
				if (_isDisposed)
					return;

				if (disposing)
				{
					_monitor.Dispose();
				}

				_isDisposed = true;

				base.Dispose(disposing);
			}
		}

		#endregion
	}
}