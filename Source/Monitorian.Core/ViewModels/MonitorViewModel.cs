using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Helper;
using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Properties;

namespace Monitorian.Core.ViewModels
{
	public class MonitorViewModel : ViewModelBase
	{
		private readonly AppControllerCore _controller;
		private IMonitor _monitor;

		public MonitorViewModel(AppControllerCore controller, IMonitor monitor)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
			this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

			LoadCustomization();
		}

		private readonly object _lock = new object();

		internal void Replace(IMonitor monitor)
		{
			if (monitor?.IsReachable is true)
			{
				lock (_lock)
				{
					this._monitor.Dispose();
					this._monitor = monitor;
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
		public double MonitorTop => _monitor.MonitorRect.Top;

		#region Customization

		private void LoadCustomization() => _controller.TryLoadCustomization(DeviceInstanceId, ref _name, ref _isUnison, ref _rangeLowest, ref _rangeHighest);
		private void SaveCustomization() => _controller.SaveCustomization(DeviceInstanceId, _name, _isUnison, _rangeLowest, _rangeHighest);

		public string Name
		{
			get => _name ?? _monitor.Description;
			set
			{
				if (SetPropertyValue(ref _name, GetValueOrNull(value)))
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
				if (SetPropertyValue(ref _isUnison, value))
					SaveCustomization();
			}
		}
		private bool _isUnison;

		/// <summary>
		/// Lowest brightness in the range of brightness
		/// </summary>
		public int RangeLowest
		{
			get => _rangeLowest;
			set
			{
				if (SetPropertyValue(ref _rangeLowest, (byte)value))
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
				if (SetPropertyValue(ref _rangeHighest, (byte)value))
					SaveCustomization();
			}
		}
		private byte _rangeHighest = 100;

		private double GetRangeRate() => Math.Abs(RangeHighest - RangeLowest) / 100D;

		/// <summary>
		/// Whether the range of brightness is changing
		/// </summary>
		public bool IsRangeChanging
		{
			get => _isRangeChanging;
			set => SetPropertyValue(ref _isRangeChanging, value);
		}
		private bool _isRangeChanging = false;

		#endregion

		#region Brightness

		public int Brightness
		{
			get => _monitor.Brightness;
			set
			{
				if (_monitor.Brightness == value)
					return;

				SetBrightness(value);

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
					RaisePropertyChanged(nameof(BrightnessSystemChanged)); // This must be prior to Brightness.
					RaisePropertyChanged(nameof(Brightness));
					RaisePropertyChanged(nameof(BrightnessSystemAdjusted));
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
			var count = Math.Floor((Brightness - RangeLowest) / size);
			int brightness = RangeLowest + (int)Math.Ceiling((count + 1) * size);

			if (brightness < RangeLowest)
				brightness = RangeLowest;
			else if (RangeHighest < brightness)
				brightness = isCycle ? RangeLowest : RangeHighest;

			SetBrightness(brightness);
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
			var count = Math.Ceiling((Brightness - RangeLowest) / size);
			int brightness = RangeLowest + (int)Math.Floor((count - 1) * size);

			if (brightness < RangeLowest)
				brightness = isCycle ? RangeHighest : RangeLowest;
			else if (RangeHighest < brightness)
				brightness = RangeHighest;

			SetBrightness(brightness);
		}

		private bool SetBrightness(int brightness)
		{
			AccessResult result;
			lock (_lock)
			{
				result = _monitor.SetBrightness(brightness);
			}

			switch (result.Status)
			{
				case AccessStatus.Succeeded:
					RaisePropertyChanged(nameof(Brightness));
					OnSucceeded();
					return true;

				default:
					_controller.OnMonitorAccessFailed(result);

					switch (result.Status)
					{
						case AccessStatus.DdcFailed:
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
		private short _controllableCount = InitialCount;
		private const short InitialCount = 3;
		private const short NormalCount = 5;

		private void OnSucceeded()
		{
			if (_controllableCount < NormalCount)
			{
				var formerCount = _controllableCount;
				_controllableCount = NormalCount;
				if (formerCount <= 0)
				{
					RaisePropertyChanged(nameof(IsControllable));
					RaisePropertyChanged(nameof(Message));
				}

				_isConfirmed = true;
			}
		}

		private void OnFailed()
		{
			if (--_controllableCount == 0)
			{
				RaisePropertyChanged(nameof(IsControllable));
				RaisePropertyChanged(nameof(Message));
			}
		}

		public string Message
		{
			get
			{
				if (0 < _controllableCount)
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
				if (SetPropertyValue(ref _isByKey, value))
					RaisePropertyChanged(nameof(IsSelectedByKey));
			}
		}
		private bool _isByKey;

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (SetPropertyValue(ref _isSelected, value))
					RaisePropertyChanged(nameof(IsSelectedByKey));
			}
		}
		private bool _isSelected;

		public bool IsSelectedByKey => IsSelected && IsByKey;

		#endregion

		public bool IsTarget
		{
			get => _isTarget;
			set => SetPropertyValue(ref _isTarget, value);
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