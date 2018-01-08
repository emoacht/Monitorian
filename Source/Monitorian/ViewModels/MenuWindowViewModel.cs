using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Models;
using StartupAgency;

namespace Monitorian.ViewModels
{
	public class MenuWindowViewModel : ViewModelBase
	{
		private readonly MainController _controller;
		public Settings Settings => _controller.Settings;
		private StartupAgent _startupAgent => _controller.StartupAgent;

		public MenuWindowViewModel(MainController controller)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
		}

		#region License

		private const string LicenseFileName = "LICENSE.txt";

		public void OpenLicense()
		{
			Task.Run(() =>
			{
				var licenseFileContent = DocumentService.ReadEmbeddedFile($"Resources.{LicenseFileName}");
				var licenseFilePath = DocumentService.SaveAsHtml(LicenseFileName, licenseFileContent);

				Process.Start(licenseFilePath);
			});
		}

		#endregion

		#region Startup

		public bool CanRegister => _startupAgent.CanRegister();

		public bool IsRegistered
		{
			get
			{
				if (!_isRegistered.HasValue)
				{
					_isRegistered = _startupAgent.IsRegistered();
				}
				return _isRegistered.Value;
			}
			set
			{
				if (_isRegistered == value)
					return;

				if (value)
				{
					_startupAgent.Register();
				}
				else
				{
					_startupAgent.Unregister();
				}
				_isRegistered = value;
				RaisePropertyChanged();
			}
		}
		private bool? _isRegistered;

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