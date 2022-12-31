using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Monitorian.Core.ViewModels;

namespace Monitorian.Core.Views
{
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

			if (this.Resources["Content"] is not StackPanel content)
				return;

			if (_additionalItems is { Length: > 0 })
			{
				foreach (var (item, index) in _additionalItems)
				{
					int i = index ?? Math.Max(0, content.Children.Count - 1);
					content.Children.Insert(i, item);
				}
			}

			this.Content = content;
			MenuWindow.EnsureFlowDirection(this);
		}
	}
}