using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monitorian.Core.Models
{
	public class LanguageService
	{
		public static IReadOnlyCollection<string> Options => new[] { Option };
		private const string Option = "/lang";

		private static readonly Lazy<CultureInfo> _culture = new(() =>
		{
			var arguments = AppKeeper.DefinedArguments;

			int i = 0;
			while (i < arguments.Count - 1)
			{
				if (string.Equals(arguments[i], Option, StringComparison.OrdinalIgnoreCase))
				{
					var cultureName = arguments[i + 1];
					return CultureInfo.GetCultures(CultureTypes.AllCultures)
						.FirstOrDefault(x => string.Equals(x.Name, cultureName, StringComparison.OrdinalIgnoreCase));
				}
				i++;
			}
			return null;
		});

		/// <summary>
		/// Switches default and current threads' culture.
		/// </summary>
		/// <returns>True if successfully switches the culture</returns>
		public static bool SwitchDefault()
		{
			var culture = _culture.Value;
			if (culture is null)
				return false;

			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
			return Switch();
		}

		/// <summary>
		/// Switches current thread's culture.
		/// </summary>
		/// <returns>True if successfully switches the culture</returns>
		public static bool Switch()
		{
			var culture = _culture.Value;
			if (culture is null)
				return false;

			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
			return true;
		}
	}
}