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
		private static IReadOnlyDictionary<string, string> PreparedCulturePairs => new Dictionary<string, string>
		{
			{ "/en", "en-US" },
			{ "/ja", "ja-JP" }
		};

		public static IReadOnlyCollection<string> Options => PreparedCulturePairs.Keys.ToArray();

		private static readonly Lazy<CultureInfo> _culture = new Lazy<CultureInfo>(() =>
		{
			var preparedCulturePairs = PreparedCulturePairs;
			var supportedCultureNames = new HashSet<string>(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => x.Name));

			return AppKeeper.DefinedArguments
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => (Success: preparedCulturePairs.TryGetValue(x.ToLower(), out string value) && supportedCultureNames.Contains(value), CultureName: value))
				.Where(x => x.Success)
				.Select(x => new CultureInfo(x.CultureName))
				.FirstOrDefault();
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