using System;
using System.Threading.Tasks;

namespace Monitorian.Core.Models;

/// <summary>
/// A wrapper class of <see cref="Windows.System.Launcher"/>
/// </summary>
public static class Launcher
{
	/// <summary>
	/// Launches the default app associated with the URI scheme name for a specified URI.
	/// </summary>
	/// <param name="uri">Uri</param>
	/// <returns>True if successfully launches</returns>
	/// <remarks>
	/// <see cref="Windows.System.Launcher"/> is only available on Windows 10 (version 10.0.10240.0)
	/// or greater.
	/// </remarks>
	public static Task<bool> LaunchAsync(Uri uri)
	{
		return Windows.System.Launcher.LaunchUriAsync(uri).AsTask();
	}
}