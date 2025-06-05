using System;

namespace Monitorian.Core.Views;

/// <summary>
/// Scroll input (mouse wheel, touchpad swipe)
/// </summary>
[Flags]
public enum ScrollInput
{
	/// <summary>
	/// None
	/// </summary>
	None = 0,

	/// <summary>
	/// Mouse vertical wheel
	/// </summary>
	MouseVerticalWheel = 1,

	/// <summary>
	/// Mouse horizontal wheel
	/// </summary>
	MouseHorizontalWheel = 2,

	/// <summary>
	/// Touchpad vertical swipe
	/// </summary>
	TouchpadVerticalSwipe = 4,

	/// <summary>
	/// Touchpad horizontal swipe
	/// </summary>
	TouchpadHorizontalSwipe = 8
}