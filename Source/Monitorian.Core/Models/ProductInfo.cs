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
	/// <summary>
	/// Product information
	/// </summary>
	public static class ProductInfo
	{
		private static readonly Assembly _assembly = Assembly.GetEntryAssembly();

		/// <summary>
		/// Version (from entry assembly)
		/// </summary>
		public static Version Version { get; } = _assembly.GetName().Version;

		/// <summary>
		/// Product (from entry assembly)
		/// </summary>
		public static string Product { get; } = _assembly.GetAttribute<AssemblyProductAttribute>().Product;

		/// <summary>
		/// Title (from entry assembly)
		/// </summary>
		public static string Title
		{
			get => _title ??= _assembly.GetAttribute<AssemblyTitleAttribute>().Title;
			set => _title = value;
		}
		private static string _title;

		/// <summary>
		/// Version of core library (from executing assembly)
		/// </summary>
		public static Version CoreVersion => Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// Product of core library (from executing assembly)
		/// </summary>
		public static string CoreProduct => Assembly.GetExecutingAssembly().GetAttribute<AssemblyProductAttribute>().Product;

		/// <summary>
		/// Startup task ID
		/// </summary>
		public static string StartupTaskId => GetAppSettings();

		private static TAttribute GetAttribute<TAttribute>(this Assembly assembly) where TAttribute : Attribute =>
			(TAttribute)Attribute.GetCustomAttribute(assembly, typeof(TAttribute));

		private static string GetAppSettings([CallerMemberName] string key = null) =>
			ConfigurationManager.AppSettings[key];
	}
}