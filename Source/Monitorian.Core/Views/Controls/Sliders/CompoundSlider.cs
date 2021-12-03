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
		private object _source;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_source = BindingOperations.GetBindingExpression(this, ValueProperty).DataItem;

			this.Unloaded += (_, _) => _source = null;
		}

		#region Unison

		private static event EventHandler<(object source, double delta, bool update)> Moved; // Static event

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

						if (instance.IsUnison)
						{
							if (instance.IsFocused)
							{
								// This route is handled by OnValueChanged method.
								instance._update = true;
							}
							else
							{
								// As DependencyPropertyChangedEventArgs.OldValue property is not always reliable,
								// this route must be called before this instance's Value property is updated
								// in order to obtain old value from that Value property.
								Moved?.Invoke(instance, (instance._source, (int)e.NewValue - instance.Value, update: true));
							}
						}
					}));

		private bool _update;

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);

			var update = _update;
			_update = false;

			if (IsUnison && this.IsFocused)
			{
				var delta = (newValue - oldValue) / GetRangeRate();
				Moved?.Invoke(this, (_source, delta, update: update));
			}
		}

		private double? _brightnessProtruded = null;

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			_brightnessProtruded = null; // Reset
		}

		private void OnMoved(object sender, (object source, double delta, bool update) e)
		{
			if (ReferenceEquals(this, sender) || ReferenceEquals(this._source, e.source))
				return;

			if (e.delta != 0D)
			{
				_brightnessProtruded ??= this.Value;
				_brightnessProtruded += e.delta * GetRangeRate();

				UpdateValue(_brightnessProtruded.Value);
			}

			if ((e.delta == 0D) || e.update)
			{
				base.EnsureUpdateSource();
			}
		}

		public override void EnsureUpdateSource()
		{
			base.EnsureUpdateSource();

			Moved?.Invoke(this, (_source, 0D, update: true));
		}

		#endregion
	}
}