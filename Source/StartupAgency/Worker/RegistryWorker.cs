using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StartupAgency.Worker
{
	/// <summary>
	/// Registry worker
	/// </summary>
	internal class RegistryWorker : IStartupWorker
	{
		private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run"; // HKCU
		private const string Option = "/startup";

		private readonly string _title;
		private readonly string _pathWithOption;

		public RegistryWorker(string title, string path)
		{
			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentNullException(nameof(title));
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			this._title = title;
			this._pathWithOption = $"{path} {Option}";
		}

		public bool IsStartedOnSignIn()
		{
			return Environment.GetCommandLineArgs().Skip(1).Contains(Option);
		}

		public bool CanRegister() => true;

		public bool IsRegistered()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, false))
			{
				var existingValue = key.GetValue(_title) as string;
				return string.Equals(existingValue, _pathWithOption, StringComparison.OrdinalIgnoreCase);
			}
		}

		public bool Register()
		{
			if (IsRegistered())
				return false;

			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				key.SetValue(_title, _pathWithOption, RegistryValueKind.String);
				return true;
			}
		}

		public void Unregister()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				if (!key.GetValueNames().Contains(_title)) // The content of value doesn't matter.
					return;

				key.DeleteValue(_title, false);
			}
		}
	}
}