using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Models;

namespace Monitorian.ViewModels
{
	public class MenuWindowViewModel : ViewModelBase
	{
		private readonly MainController _controller;
		public Settings Settings => _controller.Settings;

		public MenuWindowViewModel(MainController controller)
		{
			if (controller == null)
				throw new ArgumentNullException(nameof(controller));

			this._controller = controller;

			_isRegistered = RegistryService.IsRegistered();
		}

		public bool IsRegistered
		{
			get { return _isRegistered; }
			set
			{
				if (_isRegistered == value)
					return;

				if (value)
				{
					RegistryService.Register();
				}
				else
				{
					RegistryService.Unregister();
				}
				_isRegistered = value;
				RaisePropertyChanged();
			}
		}
		private bool _isRegistered;

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