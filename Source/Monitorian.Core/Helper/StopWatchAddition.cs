using System;
using System.Diagnostics;

namespace Monitorian.Core.Helper;

/// <summary>
/// Additional methods for <see cref="System.Diagnostics.Stopwatch"/>
/// </summary>
public static class StopWatchAddition
{
	public static long GetTimeStamp() => Stopwatch.GetTimestamp();

	public static TimeSpan GetElapsedTime(long startingTimestamp)
	{
		var seconds = GetElapsedTotalSeconds(startingTimestamp);
		return new TimeSpan((long)(seconds * TimeSpan.TicksPerSecond));
	}

	public static double GetElapsedTotalSeconds(long startingTimestamp)
	{
		return (double)(Stopwatch.GetTimestamp() - startingTimestamp) / Stopwatch.Frequency;
	}
}