using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Models;

namespace Monitorian.Core.ViewModels
{
	public class MenuWindowViewModel : ViewModelBase
	{
		private readonly AppControllerCore _controller;
		public SettingsCore Settings => _controller.Settings;

		public MenuWindowViewModel(AppControllerCore controller)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));

			CleanLicense();
		}

		#region License

		private const string LicenseFileName = "License.txt";
		private static bool _licenseFileExists = true; // Default

		public void OpenLicense()
		{
			Task.Run(() =>
			{
				var licenseFileBody = DocumentService.ReadEmbeddedFile(LicenseFileName);
				var licenseFilePath = DocumentService.SaveTempFileAsHtml(LicenseFileName, ProductInfo.Product, licenseFileBody);
				Process.Start(licenseFilePath);
				_licenseFileExists = true;
			});
		}

		private void CleanLicense()
		{
			if (!_licenseFileExists)
				return;

			Task.Run(() => _licenseFileExists = DocumentService.DeleteTempFile(LicenseFileName, TimeSpan.FromHours(1)));
		}

		#endregion

		#region Startup

		public bool CanRegister => _controller.StartupAgent.CanRegister();

		public bool IsRegistered
		{
			get
			{
				if (!_isRegistered.HasValue)
				{
					_isRegistered = _controller.StartupAgent.IsRegistered();
				}
				return _isRegistered.Value;
			}
			set
			{
				if (_isRegistered == value)
					return;

				if (value)
				{
					_controller.StartupAgent.Register();
				}
				else
				{
					_controller.StartupAgent.Unregister();
				}
				_isRegistered = value;
				OnPropertyChanged();
			}
		}
		private bool? _isRegistered;

		#endregion

		#region Accent color

		public bool IsAccentColorSupported => _controller.WindowPainter.IsAccentColorSupported;

		#endregion

		public event EventHandler CloseAppRequested;

		/// <summary>
		/// Closes this application.
		/// </summary>
		public void CloseApp() => CloseAppRequested?.Invoke(this, EventArgs.Empty);

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				CloseAppRequested = null;
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion
	}
}