using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Monitorian.Core.ViewModels;

namespace Monitorian.Core.Views;

public partial class DevSection : UserControl
{
	private readonly (UIElement, int?)[] _additionalItems;

	public DevSection(AppControllerCore controller, IEnumerable<(UIElement item, int? index)> additionalItems = null)
	{
		InitializeComponent();

		this.DataContext = new DevSectionViewModel(controller);
		this._additionalItems = additionalItems?.ToArray();
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		var window = Window.GetWindow(this) as MenuWindow;
		if (window?.AppTitle is TextBlock appTitle)
		{
			appTitle.MouseDown += (_, _) => Open();
		}
	}

	private int _count = 0;
	private const int CountThreshold = 3;

	public void Open()
	{
		if (++_count != CountThreshold)
			return;

		if (this.Resources["Content"] is ControlTemplate template)
			this.Template = template;
	}

	private void ContentPanel_Initialized(object sender, EventArgs e)
	{
		if ((sender is StackPanel panel) &&
			(_additionalItems is { Length: > 0 }))
		{
			foreach (var (item, index) in _additionalItems)
			{
				int i = index ?? Math.Max(0, panel.Children.Count - 1);
				panel.Children.Insert(i, item);
			}
		}

		MenuWindow.EnsureFlowDirection(this);
	}
}