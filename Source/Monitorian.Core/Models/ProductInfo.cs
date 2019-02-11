using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models
{
	public static class ProductInfo
	{
		private static readonly Assembly _assembly = Assembly.GetEntryAssembly();

		public static Version Version { get; } = _assembly.GetName().Version;

		public static string Title { get; } = GetAttribute<AssemblyTitleAttribute>(_assembly).Title;

		private static TAttribute GetAttribute<TAttribute>(Assembly assembly) where TAttribute : Attribute =>
			(TAttribute)Attribute.GetCustomAttribute(assembly, typeof(TAttribute));

		public static string StartupTaskId => GetAppSettings();

		private static string GetAppSettings([CallerMemberName] string key = null) =>
			ConfigurationManager.AppSettings[key];
	}
}