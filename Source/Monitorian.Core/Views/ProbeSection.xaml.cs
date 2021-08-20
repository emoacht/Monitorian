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
	public partial class ProbeSection : UserControl
	{
		internal ProbeSectionViewModel ViewModel => (ProbeSectionViewModel)this.DataContext;

		private readonly UIElement[] _additionalItems;

		public ProbeSection(AppControllerCore controller, IEnumerable<UIElement> additionalItems = null)
		{
			InitializeComponent();

			this.DataContext = new ProbeSectionViewModel(controller);
			this._additionalItems = additionalItems?.ToArray();
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var window = Window.GetWindow(this) as MenuWindow;
			if (window?.AppTitle is TextBlock appTitle)
			{
				appTitle.MouseDown += (sender, e) => Open();
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

			if (_additionalItems is not null)
			{
				int index = Math.Max(0, content.Children.Count - 1);

				foreach (var item in _additionalItems)
					content.Children.Insert(index, item);
			}

			this.Content = content;

			MenuWindow.EnsureFlowDirection(this);
		}
	}
}