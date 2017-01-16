using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Models;

namespace Monitorian.ViewModels
{
	public class MenuWindowViewModel : ViewModelBase
	{
		public MenuWindowViewModel()
		{
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

		/// <summary>
		/// Closes this application.
		/// </summary>
		public void CloseApp() => App.Current.Shutdown();
	}
}