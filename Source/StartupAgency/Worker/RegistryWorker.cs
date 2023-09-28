using System;
using System.Linq;
using Microsoft.Win32;

namespace StartupAgency.Worker;

/// <summary>
/// Registry worker
/// </summary>
internal class RegistryWorker : IStartupWorker
{
	private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run"; // HKCU
	private const string Option = "/startup";

	private readonly string _name;
	private readonly string _pathWithOption;

	public RegistryWorker(string name, string path)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentNullException(nameof(path));

		this._name = name;
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
			var existingValue = key.GetValue(_name) as string;
			return string.Equals(existingValue, _pathWithOption, StringComparison.OrdinalIgnoreCase);
		}
	}

	public bool Register()
	{
		if (IsRegistered())
			return false;

		using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
		{
			key.SetValue(_name, _pathWithOption, RegistryValueKind.String);
			return true;
		}
	}

	public void Unregister()
	{
		using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
		{
			if (!key.GetValueNames().Contains(_name)) // The content of value doesn't matter.
				return;

			key.DeleteValue(_name, false);
		}
	}
}