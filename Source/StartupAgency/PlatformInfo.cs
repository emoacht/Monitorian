using System;
using Windows.ApplicationModel;

namespace StartupAgency;

/// <summary>
/// Platform information
/// </summary>
public static class PlatformInfo
{
	/// <summary>
	/// Whether this assembly is packaged in AppX package
	/// </summary>
	public static bool IsPackaged => _isPackaged.Value;
	private static readonly Lazy<bool> _isPackaged = new(() => IsPackagedWithName());

	private static bool IsPackagedWithName()
	{
		try
		{
			var package = Package.Current;
			return !string.IsNullOrEmpty(package.Id.FamilyName);
		}
		catch (InvalidOperationException ex) when ((uint)ex.HResult is 0x80073D54)
		{
			// Message: The process has no package identity. (Exception from HRESULT: 0x80073D54)
			// 0x80073D54 means 0x3D54 -> 15700 -> APPMODEL_ERROR_NO_PACKAGE
			return false;
		}
		catch (AggregateException ex) when (ex.InnerException is ArgumentException)
		{
			// Message: The parameter is incorrect.
			// This error occurs when StartupTask TaskId is not defined in AppxManifest.xml.
			throw;
		}
	}
}