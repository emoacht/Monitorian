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
		private readonly IMonitor _monitor;

		public MonitorViewModel(IMonitor monitor)
		{
			this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
		}

		public string Description => _monitor.Description;
		public string DeviceInstanceId => _monitor.DeviceInstanceId;
		public byte DisplayIndex => _monitor.DisplayIndex;
		public byte MonitorIndex => _monitor.MonitorIndex;

		#region Name

		private bool _isNameChanged;

		public string Name
		{
			get => HasName ? _name : _monitor.Description;
			set
			{
				if (SetPropertyValue(ref _name, value))
					_isNameChanged = true;
			}
		}
		private string _name;

		public bool HasName => !string.IsNullOrWhiteSpace(_name);

		public void RestoreName(string name)
		{
			this._name = name;
		}

		public bool CheckNameChanged()
		{
			if (_isNameChanged)
			{
				_isNameChanged = false;
				return true;
			}
			return false;
		}

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

		public DateTimeOffset UpdateTime { get; private set; }

		public void UpdateBrightness(int brightness = -1)
		{
			UpdateTime = DateTimeOffset.Now;

			if (_monitor.UpdateBrightness(brightness))
			{
				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
				RaisePropertyChanged(nameof(BrightnessAdjusted));
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
				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
			}
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