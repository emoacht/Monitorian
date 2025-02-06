using System.Runtime.InteropServices;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// System Metric Functions
/// </summary>
internal class SystemMetric
{
	#region Win32

	[DllImport("User32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	// The number of display monitors on a desktop.
	// GetSystemMetrics(SM_CMONITORS) counts only visible display monitors. This is different from
	// EnumDisplayMonitors, which enumerates both visible display monitors and invisible pseudo-monitors
	// that are associated with mirroring drivers. An invisible pseudo-monitor is associated with
	// a pseudo-device used to mirror application drawing for remoting or other purposes.
	// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics
	private static int SM_CMONITORS = 80;

	#endregion

	public static int GetMonitorCount() => GetSystemMetrics(SM_CMONITORS);
}