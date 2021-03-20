using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenFrame.Helper
{
	/// <summary>
	/// Additional methods for <see cref="System.Globalization.CultureInfo"/>
	/// </summary>
	internal static class CultureInfoAddition
	{
		#region Win32

		[DllImport("Kernel32.dll")]
		private static extern ushort GetUserDefaultUILanguage();

		#endregion

		public static CultureInfo UserDefaultUICulture => _userDefaultUICulture.Value;
		private static readonly Lazy<CultureInfo> _userDefaultUICulture = new(() => new(GetUserDefaultUILanguage()));
	}
}