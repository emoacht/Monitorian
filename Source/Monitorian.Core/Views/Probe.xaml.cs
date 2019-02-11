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
	public partial class Probe : UserControl
	{
		internal ProbeViewModel ViewModel => (ProbeViewModel)this.DataContext;

		public Probe()
		{
			InitializeComponent();

			this.DataContext = new ProbeViewModel();
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var window = Window.GetWindow(this) as MenuWindow;
			if (window?.AppTitle is TextBlock appTitle)
			{
				appTitle.MouseDown += (sender, e) => ViewModel.EnableProbe();
			}
		}
	}
}