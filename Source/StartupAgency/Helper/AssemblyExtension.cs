using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StartupAgency.Helper
{
	/// <summary>
	/// Extension method for <see cref="System.Reflection.Assembly"/>
	/// </summary>
	internal static class AssemblyExtension
	{
		public static string GetTitle(this Assembly source)
		{
			T getAttribute<T>(Assembly x) where T : Attribute => (T)Attribute.GetCustomAttribute(x, typeof(T));

			return getAttribute<AssemblyTitleAttribute>(source).Title;
		}
	}
}