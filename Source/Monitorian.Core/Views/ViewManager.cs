using System.Collections.Generic;
using System.Linq;

namespace Monitorian.Core.Views;

public static class ViewManager
{
	/// <summary>
	/// Factor for mouse wheel
	/// </summary>
	public static int WheelFactor { get; set; } = 5;

	public static IReadOnlyCollection<string> Options => (new[] { IconWheelOption })
		.Concat(WindowPainter.Options)
		.ToArray();

	private const string IconWheelOption = "/iconwheel";

	public static bool IsIconWheelEnabled()
	{
		return AppKeeper.StandardArguments.Select(x => x.ToLower()).Contains(IconWheelOption);
	}
}