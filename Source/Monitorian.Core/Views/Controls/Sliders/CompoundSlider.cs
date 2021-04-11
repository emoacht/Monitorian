using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Views.Controls
{
	public class CompoundSlider : ShadowSlider
	{
		#region Unison

		private static event EventHandler<double> Moved; // Static event

		public bool IsUnison
		{
			get { return (bool)GetValue(IsUnisonProperty); }
			set { SetValue(IsUnisonProperty, value); }
		}
		public static readonly DependencyProperty IsUnisonProperty =
			DependencyProperty.Register(
				"IsUnison",
				typeof(bool),
				typeof(CompoundSlider),
				new PropertyMetadata(
					false,
					(d, e) =>
					{
						var instance = (CompoundSlider)d;

						if ((bool)e.NewValue)
						{
							Moved += instance.OnMoved;
						}
						else
						{
							Moved -= instance.OnMoved;
						}
					}));

		public int ValueUnison
		{
			get { return (int)GetValue(ValueUnisonProperty); }
			set { SetValue(ValueUnisonProperty, value); }
		}
		public static readonly DependencyProperty ValueUnisonProperty =
			DependencyProperty.Register(
				"ValueUnison",
				typeof(int),
				typeof(CompoundSlider),
				new PropertyMetadata(
					0,
					(d, e) =>
					{
						var instance = (CompoundSlider)d;

						if (!instance.IsFocused && instance.IsUnison)
						{
							Moved?.Invoke(instance, (int)e.NewValue - instance.Value);
						}
					}));

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);

			if (this.IsFocused && IsUnison)
			{
				var delta = (newValue - oldValue) / GetRangeRate();
				Moved?.Invoke(this, delta);
			}
		}

		private double? _brightnessProtruded = null;

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			_brightnessProtruded = null; // Reset
		}

		private void OnMoved(object sender, double delta)
		{
			if (ReferenceEquals(this, sender))
				return;

			if (delta != 0D)
			{
				_brightnessProtruded ??= this.Value;
				_brightnessProtruded += delta * GetRangeRate();

				UpdateValue(_brightnessProtruded.Value);
			}
			else
			{
				base.UpdateSourceDeferred();
			}
		}

		protected override void UpdateSourceDeferred()
		{
			base.UpdateSourceDeferred();

			Moved?.Invoke(this, 0D);
		}

		#endregion
	}
}