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

		public bool IsControllable
		{
			get => IsAccessible && _isControllable;
			private set => SetPropertyValue(ref _isControllable, value);
		}
		private bool _isControllable = true;

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

		public int Brightness => _monitor.Brightness;

		public int BrightnessInteractive
		{
			get => Brightness;
			set
			{
				if (Brightness == value)
					return;

				SetBrightness(value);
			}
		}

		public int BrightnessSystemAdjusted => _monitor.BrightnessSystemAdjusted;
		public int BrightnessSystemChanged => Brightness;

		public bool UpdateBrightness(int brightness = -1)
		{
			if (_monitor.UpdateBrightness(brightness))
			{
				if (IsUnison && (0 <= brightness))
					RaisePropertyChanged(nameof(BrightnessSystemChanged));

				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
				RaisePropertyChanged(nameof(BrightnessSystemAdjusted));
				OnSuccess();
				return true;
			}
			OnFailure();
			return false;
		}

		public void IncrementBrightness() => IncrementBrightness(10);

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
				if (IsUnison)
					RaisePropertyChanged(nameof(BrightnessSystemChanged));

				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
				OnSuccess();
				return true;
			}
			OnFailure();
			return false;
		}

		private byte _failureCount = 0;

		private void OnSuccess()
		{
			_failureCount = 0;
		}

		private void OnFailure()
		{
			if (++_failureCount > 2)
				IsControllable = false;
		}

		#endregion

		public bool IsTarget
		{
			get => _isTarget;
			set => SetPropertyValue(ref _isTarget, value);
		}
		private bool _isTarget = false;

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