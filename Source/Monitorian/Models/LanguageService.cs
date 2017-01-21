using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monitorian.Models
{
	internal class LanguageService
	{
		private static readonly Dictionary<string, string> _cultures = new Dictionary<string, string>
		{
			{ "/en", "en-US" },
			{ "/ja", "ja-JP" }
		};

		public static readonly IReadOnlyList<string> Arguments = _cultures.Keys.ToArray();

		/// <summary>
		/// Switches this application's culture depending on given arguments.  
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns>True if successfully switched the culture</returns>
		public static bool Switch(string[] args)
		{
			foreach (var arg in args.Select(x => x.ToLower()))
			{
				if (_cultures.ContainsKey(arg) && SetCulture(_cultures[arg]))
					return true;
			}
			return false;
		}

		public static CultureInfo GetCulture() => CultureInfo.CurrentCulture;

		public static bool SetCulture(string cultureName)
		{
			CultureInfo cultureInfo;
			try
			{
				cultureInfo = new CultureInfo(cultureName);
			}
			catch (CultureNotFoundException)
			{
				Debug.WriteLine($"Failed to find culture from culture name ({cultureName}).");
				return false;
			}

			Thread.CurrentThread.CurrentCulture = cultureInfo;
			Thread.CurrentThread.CurrentUICulture = cultureInfo;
			return true;
		}
	}
}