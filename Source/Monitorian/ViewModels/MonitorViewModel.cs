using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Models.Monitor;

namespace Monitorian.ViewModels
{
	public class MonitorViewModel : ViewModelBase
	{
		private readonly MainController _controller;
		private readonly IMonitor _monitor;

		public MonitorViewModel(MainController controller, IMonitor monitor)
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

		#region Name & Unison

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

		public int BrightnessAdjusted => _monitor.BrightnessAdjusted;

		public int BrightnessUnison => Brightness;

		public DateTimeOffset UpdateTime { get; private set; }

		public void UpdateBrightness(int brightness = -1)
		{
			UpdateTime = DateTimeOffset.Now;

			if (_monitor.UpdateBrightness(brightness))
			{
				OnSuccess();

				if (IsUnison && (0 <= brightness))
					RaisePropertyChanged(nameof(BrightnessUnison));

				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
				RaisePropertyChanged(nameof(BrightnessAdjusted));
			}
			else
			{
				OnFailure();
			}
		}

		public void IncrementBrightness()
		{
			int brightness = (int)Math.Floor(Brightness / 10D) * 10 + 10;
			if (100 < brightness)
				brightness = 0;

			SetBrightness(brightness);
		}

		private void SetBrightness(int brightness)
		{
			if (_monitor.SetBrightness(brightness))
			{
				OnSuccess();
				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
			}
			else
			{
				OnFailure();
			}
		}

		private byte _failureCount = 0;

		private void OnSuccess()
		{
			_failureCount = 0;
		}

		private void OnFailure()
		{
			if (_failureCount++ > 0)
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