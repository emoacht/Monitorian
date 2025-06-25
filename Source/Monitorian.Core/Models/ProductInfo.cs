using System;
using System.Configuration;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Monitorian.Core.Models;

/// <summary>
/// Product information
/// </summary>
public static class ProductInfo
{
	private readonly struct ProductInfoBase(Assembly assembly)
	{
		public readonly Version Version = assembly.GetName().Version;
		public readonly string Product = assembly.GetAttribute<AssemblyProductAttribute>().Product;
		public readonly string Title = assembly.GetAttribute<AssemblyTitleAttribute>().Title;
	}

	private static readonly Lazy<ProductInfoBase> _instance = new(() => new(Assembly.GetEntryAssembly()));

	/// <summary>
	/// Version (from entry assembly)
	/// </summary>
	public static Version Version => _instance.Value.Version;

	/// <summary>
	/// Product (from entry assembly)
	/// </summary>
	public static string Product => _instance.Value.Product;

	/// <summary>
	/// Title (from entry assembly)
	/// </summary>
	public static string Title
	{
		get => _title ??= _instance.Value.Title;
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

	/// <summary>
	/// Project Url
	/// </summary>
	public static string ProjectUrl => GetAppSettings();

	private static TAttribute GetAttribute<TAttribute>(this Assembly assembly) where TAttribute : Attribute =>
		(TAttribute)Attribute.GetCustomAttribute(assembly, typeof(TAttribute));

	private static string GetAppSettings([CallerMemberName] string key = null) =>
		ConfigurationManager.AppSettings[key];
}