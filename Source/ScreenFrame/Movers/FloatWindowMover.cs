using System;
using System.Windows;

using ScreenFrame.Helper;

namespace ScreenFrame.Movers;

/// <summary>
/// Mover of <see cref="System.Windows.Window"/> which implements functions for float window
/// </summary>
public class FloatWindowMover : BasicWindowMover
{
	private readonly Point _pivot;
	private readonly double _pivotWidth;
	private readonly double _pivotHeight;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="window">Window to be moved</param>
	/// <param name="pivot">Pivot point to be referred</param>
	public FloatWindowMover(Window window, Point pivot) : base(window)
	{
		this._pivot = pivot;
		_pivotWidth = 0;
		_pivotHeight = 0;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="window">Window to be moved</param>
	/// <param name="pivot">Pivot rectangle to be referred</param>
	public FloatWindowMover(Window window, Rect pivot) : base(window)
	{
		this._pivot = pivot.Location;
		_pivotWidth = Math.Max(0, pivot.Width);
		_pivotHeight = Math.Max(0, pivot.Height);
	}

	/// <summary>
	/// Alignment of pivot
	/// </summary>
	public override PivotAlignment PivotAlignment { get; protected set; }

	/// <summary>
	/// Gets Per-Monitor DPI of the monitor.
	/// </summary>
	/// <returns>DPI information</returns>
	protected override DpiScale GetDpi() => VisualTreeHelperAddition.GetDpi(_pivot);

	/// <summary>
	/// Attempts to get the adjacent location using specified window width and height.
	/// </summary>
	/// <param name="windowWidth">Window width</param>
	/// <param name="windowHeight">Window height</param>
	/// <param name="location">Location of window</param>
	/// <returns>True if successfully gets</returns>
	protected override bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Rect location) =>
		TryGetAdjacentLocationToPivot(windowWidth, windowHeight, out location);

	/// <summary>
	/// Attempts to get the adjacent location to pivot point using specified window width and height.
	/// </summary>
	/// <param name="windowWidth">Window width</param>
	/// <param name="windowHeight">Window height</param>
	/// <param name="location">Location of window</param>
	/// <returns>True if successfully gets</returns>
	protected bool TryGetAdjacentLocationToPivot(double windowWidth, double windowHeight, out Rect location)
	{
		if (!WindowHelper.TryGetTaskbar(out _, out TaskbarAlignment taskbarAlignment))
		{
			location = default;
			return false;
		}

		var isLeftToRight = !CultureInfoAddition.UserDefaultUICulture.TextInfo.IsRightToLeft;

		PivotAlignment = (taskbarAlignment, isLeftToRight) switch
		{
			(TaskbarAlignment.Top, true) => PivotAlignment.TopRight,
			(TaskbarAlignment.Top, false) => PivotAlignment.TopLeft,
			(TaskbarAlignment.Bottom, true) => PivotAlignment.BottomRight,
			(TaskbarAlignment.Bottom, false) => PivotAlignment.BottomLeft,
			(TaskbarAlignment.Left, { }) => PivotAlignment.BottomLeft,
			(TaskbarAlignment.Right, { }) => PivotAlignment.BottomRight,
			_ => default
		};

		var x = _pivot.X;
		var y = _pivot.Y;

		var offsetX = (_pivotWidth == 0) ? 1 : 0;
		var offsetY = (_pivotHeight == 0) ? 1 : 0;

		switch (PivotAlignment)
		{
			case PivotAlignment.TopLeft:
				x += _pivotWidth + offsetX;
				y += offsetY;
				break;
			case PivotAlignment.TopRight:
				x -= (windowWidth + offsetX);
				y += offsetY;
				break;
			case PivotAlignment.BottomLeft:
				x += _pivotWidth + offsetX;
				y += _pivotHeight - (windowHeight + offsetY);
				break;
			case PivotAlignment.BottomRight:
				x -= (windowWidth + offsetX);
				y += _pivotHeight - (windowHeight + offsetY);
				break;
		}
		location = new Rect(x, y, windowWidth, windowHeight);
		return true;
	}
}