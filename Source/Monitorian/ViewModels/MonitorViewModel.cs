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
			if (monitor == null)
				throw new ArgumentNullException(nameof(monitor));

			this._monitor = monitor;
		}

		public string Description => _monitor.Description;
		public string DeviceInstanceId => _monitor.DeviceInstanceId;
		public byte DisplayIndex => _monitor.DisplayIndex;
		public byte MonitorIndex => _monitor.MonitorIndex;

		public string Name
		{
			get { return HasName ? _name : _monitor.Description; }
			set { SetPropertyValue(ref _name, value); }
		}
		private string _name;

		public bool HasName => !string.IsNullOrWhiteSpace(_name);

		public int Brightness => _monitor.Brightness;

		public int BrightnessInteractive
		{
			get { return Brightness; }
			set
			{
				if (Brightness == value)
					return;

				SetBrightness(value);
			}
		}

		public DateTime UpdateTime { get; private set; }

		public void UpdateBrightness(int brightness = -1)
		{
			UpdateTime = DateTime.Now;

			if (_monitor.UpdateBrightness(brightness))
			{
				RaisePropertyChanged(nameof(Brightness));
				RaisePropertyChanged(nameof(BrightnessInteractive));
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

		public bool IsTarget
		{
			get { return _isTarget; }
			set { SetPropertyValue(ref _isTarget, value); }
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