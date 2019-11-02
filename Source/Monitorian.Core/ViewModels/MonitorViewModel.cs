using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.ViewModels
{
	public class MonitorViewModel : ViewModelBase
	{
		private readonly AppControllerCore _controller;
		private readonly IMonitor _monitor;

		public MonitorViewModel(AppControllerCore controller, IMonitor monitor)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
			this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

			this._controller.TryLoadNameUnison(DeviceInstanceId, ref _name, ref _isUnison);
		}

		public string DeviceInstanceId => _monitor.DeviceInstanceId;
		public string Description => _monitor.Description;
		public byte DisplayIndex => _monitor.DisplayIndex;
		public byte MonitorIndex => _monitor.MonitorIndex;
		public bool IsAccessible => _monitor.IsAccessible;

		#region Name/Unison

		public string Name
		{
			get => _name ?? _monitor.Description;
			set
			{
				if (SetPropertyValue(ref _name, GetValueOrNull(value)))
					_controller.SaveNameUnison(DeviceInstanceId, _name, _isUnison);
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
					_controller.SaveNameUnison(DeviceInstanceId, _name, _isUnison);
			}
		}
		private bool _isUnison;

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
			if (_monitor.UpdateBrightness(brightness))
			{
				RaisePropertyChanged(nameof(BrightnessSystemChanged)); // This must be prior to Brightness.
				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessSystemAdjusted));
				OnSuccess();
				return true;
			}
			OnFailure();
			return false;
		}

		public void IncrementBrightness()
		{
			IncrementBrightness(10);

			if (IsSelected)
				_controller.SaveMonitorUserChanged(this);
		}

		public void IncrementBrightness(int tickSize, bool isCycle = true)
		{
			int brightness = (Brightness / tickSize) * tickSize + tickSize;
			if (100 < brightness)
				brightness = isCycle ? 0 : 100;

			SetBrightness(brightness);
		}

		public void DecrementBrightness(int tickSize, bool isCycle = true)
		{
			int brightness = (Brightness / tickSize) * tickSize - tickSize;
			if (brightness < 0)
				brightness = isCycle ? 100 : 0;

			SetBrightness(brightness);
		}

		private bool SetBrightness(int brightness)
		{
			if (_monitor.SetBrightness(brightness))
			{
				RaisePropertyChanged(nameof(Brightness));
				OnSuccess();
				return true;
			}
			OnFailure();
			return false;
		}

		#endregion

		#region Controllable

		public bool IsControllable => IsAccessible && (_currentCount > 0);

		private short _currentCount = NormalCount;
		private const short NormalCount = 5;

		private void OnSuccess()
		{
			if (_currentCount == NormalCount)
				return;

			var formerCount = _currentCount;
			_currentCount = NormalCount;
			if (formerCount <= 0)
				RaisePropertyChanged(nameof(IsControllable));
		}

		private void OnFailure()
		{
			if (--_currentCount == 0)
				RaisePropertyChanged(nameof(IsControllable));
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

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
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

		#endregion
	}
}