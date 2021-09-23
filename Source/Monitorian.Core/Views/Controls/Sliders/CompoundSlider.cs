using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Controls
{
	public class CompoundSlider : ShadowSlider
	{
		#region Unison

		private static event EventHandler<(object source, double delta)> Moved; // Static event

		private object _source;

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
						instance._source ??= BindingOperations.GetBindingExpression(d, IsUnisonProperty).DataItem;

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
							Moved?.Invoke(instance, (instance._source, (int)e.NewValue - instance.Value));
						}
					}));

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);

			if (this.IsFocused && IsUnison)
			{
				var delta = (newValue - oldValue) / GetRangeRate();
				Moved?.Invoke(this, (_source, delta));
			}
		}

		private double? _brightnessProtruded = null;

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			_brightnessProtruded = null; // Reset
		}

		private void OnMoved(object sender, (object source, double delta) e)
		{
			if (ReferenceEquals(this, sender) || ReferenceEquals(this._source, e.source))
				return;

			if (e.delta != 0D)
			{
				_brightnessProtruded ??= this.Value;
				_brightnessProtruded += e.delta * GetRangeRate();

				UpdateValue(_brightnessProtruded.Value);
			}
			else
			{
				base.ExecuteUpdateSource();
			}
		}

		protected override void ExecuteUpdateSource()
		{
			base.ExecuteUpdateSource();

			Moved?.Invoke(this, (_source, 0D));
		}

		#endregion
	}
}