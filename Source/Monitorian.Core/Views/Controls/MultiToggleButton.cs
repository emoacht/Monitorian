using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Monitorian.Core.Views.Controls
{
	public class MultiToggleButton : ToggleButton
	{
		public object SubContent
		{
			get { return (object)GetValue(SubContentProperty); }
			set { SetValue(SubContentProperty, value); }
		}
		public static readonly DependencyProperty SubContentProperty =
			DependencyProperty.Register(
				"SubContent",
				typeof(object),
				typeof(MultiToggleButton),
				new PropertyMetadata((object)null));

		public bool IsCheckable
		{
			get { return (bool)GetValue(IsCheckableProperty); }
			set { SetValue(IsCheckableProperty, value); }
		}
		public static readonly DependencyProperty IsCheckableProperty =
			MenuItem.IsCheckableProperty.AddOwner(
				typeof(MultiToggleButton),
				new PropertyMetadata(
					false,
					(d, e) => ((MultiToggleButton)d).IsChecked = false));

		static MultiToggleButton()
		{
			ToggleButton.IsCheckedProperty.OverrideMetadata(
				typeof(MultiToggleButton),
				new FrameworkPropertyMetadata(
					false,
					null,
					(d, baseValue) => ((MultiToggleButton)d).IsCheckable ? baseValue : false));
		}
	}
}