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

		public ProbeSection(AppControllerCore controller)
		{
			InitializeComponent();

			this.DataContext = new ProbeSectionViewModel(controller);
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
			if (++_count == CountThreshold)
			{
				if (this.Resources["Content"] is FrameworkElement content)
					this.Content = content;
			}
		}
	}
}