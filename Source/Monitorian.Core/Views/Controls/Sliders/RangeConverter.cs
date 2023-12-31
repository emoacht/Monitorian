using System;

namespace Monitorian.Core.Views.Controls;

public static class RangeConverter
{
	/// <summary>
	/// Attempts to convert a specified value to the level (from 0 to 1) within a given range.
	/// </summary>
	/// <param name="value">Value</param>
	/// <param name="lowest">The lowest value of the range</param>
	/// <param name="highest">The highest value of the range</param>
	/// <param name="level">Level</param>
	/// <returns>True if the value is within the range</returns>
	public static bool TryConvertToLevel(in double value, in double lowest, in double highest, out double level)
	{
		if (lowest >= highest)
			throw new ArgumentException("Highest must be higher than lowest.");

		if (value < lowest)
		{
			level = 0;
			return false;
		}
		if (value > highest)
		{
			level = 1;
			return false;
		}
		level = (value - lowest) / (highest - lowest);
		return true;
	}

	/// <summary>
	/// Attempts to convert a specified level (from 0 to 1) within a given range to the value.
	/// </summary>
	/// <param name="level">Level</param>
	/// <param name="lowest">The lowest value of the range</param>
	/// <param name="highest">The highest value of the range</param>
	/// <param name="value">Value</param>
	/// <returns>True if the level is within 0 to 1</returns>
	public static bool TryConvertFromLevel(in double level, in double lowest, in double highest, out double value)
	{
		if (lowest >= highest)
			throw new ArgumentException("Highest must be higher than lowest.");

		if (level < 0)
		{
			value = lowest;
			return false;
		}
		if (level > 1)
		{
			value = highest;
			return false;
		}
		value = (highest - lowest) * level + lowest;
		return true;
	}
}