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

		public static IReadOnlyList<string> Arguments => PreparedCulturePairs.Keys.ToArray();

		private static CultureInfo _culture = null;

		/// <summary>
		/// Switches this application's culture depending on given arguments.  
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns>True if successfully switched the culture</returns>
		public static bool Switch(IEnumerable<string> args)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			string cultureName;
			var allCultureNames = new HashSet<string>(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => x.Name));

			foreach (var arg in args
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.ToLower()))
			{
				if (PreparedCulturePairs.TryGetValue(arg, out cultureName) && allCultureNames.Contains(cultureName))
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
		/// <returns>True if successfully switched the culture</returns>
		public static bool Switch()
		{
			if (_culture == null)
				return false;

			Thread.CurrentThread.CurrentCulture = _culture;
			Thread.CurrentThread.CurrentUICulture = _culture;
			return true;
		}
	}
}