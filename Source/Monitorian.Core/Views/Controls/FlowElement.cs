using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Monitorian.Core.Models;
using ScreenFrame;

namespace Monitorian.Core.Views.Controls;

public static class FlowElement
{
	/// <summary>
	/// Ensures the contents in a specified logical tree flow in the same direction as the texts of
	/// language resources. 
	/// </summary>
	/// <param name="rootControl">Root control of the logical tree</param>
	public static void EnsureFlowDirection(ContentControl rootControl)
	{
		if (!LanguageService.IsResourceRightToLeft)
			return;

		var resourceValues = new HashSet<string>(LanguageService.ResourceDictionary.Values);

		foreach (var itemControl in LogicalTreeHelperAddition.EnumerateDescendants<ContentControl>(rootControl)
			.Select(x => x.Content as ButtonBase)
			.Where(x => x is not null))
		{
			FlowElement.SetVisibility(itemControl, Visibility.Visible);

			if (resourceValues.Contains(itemControl.Content))
				itemControl.FlowDirection = FlowDirection.RightToLeft;
		}
	}

	public static Visibility GetVisibility(DependencyObject obj)
	{
		return (Visibility)obj.GetValue(VisibilityProperty);
	}
	public static void SetVisibility(DependencyObject obj, Visibility value)
	{
		obj.SetValue(VisibilityProperty, value);
	}
	public static readonly DependencyProperty VisibilityProperty =
		DependencyProperty.RegisterAttached(
			"Visibility",
			typeof(Visibility),
			typeof(FlowElement),
			new PropertyMetadata(Visibility.Collapsed));
}