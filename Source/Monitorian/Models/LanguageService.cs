using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monitorian.Models
{
	internal class LanguageService
	{
		private static IReadOnlyDictionary<string, string> PreparedCulturePairs => new Dictionary<string, string>
		{
			{ "/en", "en-US" },
			{ "/ja", "ja-JP" }
		};

		public static IReadOnlyCollection<string> Options => PreparedCulturePairs.Keys.ToArray();

		private static CultureInfo _culture = null;

		/// <summary>
		/// Switches default and current threads' culture.
		/// </summary>
		/// <returns>True if successfully switches the culture</returns>
		public static bool SwitchDefault()
		{
			var supportedCultureNames = new HashSet<string>(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => x.Name));

			foreach (var arg in Environment.GetCommandLineArgs().Skip(1)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.ToLower()))
			{
				if (PreparedCulturePairs.TryGetValue(arg, out string cultureName) && supportedCultureNames.Contains(cultureName))
				{
					_culture = new CultureInfo(cultureName);

					CultureInfo.DefaultThreadCurrentCulture = _culture;
					CultureInfo.DefaultThreadCurrentUICulture = _culture;
					break;
				}
			}

			return Switch();
		}

		/// <summary>
		/// Switches current thread's culture.
		/// </summary>
		/// <returns>True if successfully switches the culture</returns>
		public static bool Switch()
		{
			if (_culture is null)
				return false;

			Thread.CurrentThread.CurrentCulture = _culture;
			Thread.CurrentThread.CurrentUICulture = _culture;
			return true;
		}
	}
}