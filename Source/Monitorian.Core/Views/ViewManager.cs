using System.Collections.Generic;
using System.Linq;

namespace Monitorian.Core.Views;

public static class ViewManager
{
	/// <summary>
	/// Factor for mouse wheel
	/// </summary>
	public static int WheelFactor { get; set; } = 5;

	internal static ScrollInput InvertsScrollDirection
	{
		set
		{
			InvertsMouseVerticalWheel = value.HasFlag(ScrollInput.MouseVerticalWheel);
			InvertsMouseHorizontalWheel = value.HasFlag(ScrollInput.MouseHorizontalWheel);
			InvertsTouchpadVerticalSwipe = value.HasFlag(ScrollInput.TouchpadVerticalSwipe);
			InvertsTouchpadHorizontalSwipe = value.HasFlag(ScrollInput.TouchpadHorizontalSwipe);
		}
	}

	public static bool InvertsMouseVerticalWheel { get; private set; }
	public static bool InvertsMouseHorizontalWheel { get; private set; }
	public static bool InvertsTouchpadVerticalSwipe { get; private set; }
	public static bool InvertsTouchpadHorizontalSwipe { get; private set; }

	public static IReadOnlyCollection<string> Options => (new[] { IconWheelOption })
		.Concat(WindowPainter.Options)
		.ToArray();

	private const string IconWheelOption = "/iconwheel";

	public static bool IsIconWheelEnabled()
	{
		return AppKeeper.StandardArguments.Select(x => x.ToLower()).Contains(IconWheelOption);
	}
}