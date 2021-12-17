using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Controls
{
	public class CompoundSlider : ShadowSlider
	{
		#region Type

		private class Item
		{
			public object Source { get; set; }
			public List<CompoundSlider> Sliders { get; } = new();
			public double? BrightnessProtruded { get; set; }

			public Item(object source) => this.Source = source;
		}

		private class ItemHolder
		{
			private readonly List<Item> _items = new();

			public void Add(object source, CompoundSlider slider)
			{
				var item = _items.FirstOrDefault(x => ReferenceEquals(x.Source, source));
				if (item is null)
				{
					item = new Item(source);
					_items.Add(item);
				}
				else if (item.Sliders.Any(x => ReferenceEquals(x, slider)))
					return;

				item.Sliders.Add(slider);
			}

			public void Remove(object source, CompoundSlider slider)
			{
				var item = _items.FirstOrDefault(x => ReferenceEquals(x.Source, source));
				if (item is null)
					return;

				item.Sliders.Remove(slider);
				if (item.Sliders.Any())
					return;

				item.Source = null;
				_items.Remove(item);
			}

			public bool TryGetItem(object source, out Item item)
			{
				item = _items.FirstOrDefault(x => ReferenceEquals(x.Source, source));
				return (item is not null);
			}
		}

		#endregion

		private static readonly ItemHolder _holder = new();

		private object _source; // Binding source

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_source = this.DataContext;
			if (_source is null)
				throw new InvalidOperationException("The binding source must not be null.");

			_holder.Add(_source, this);

			this.Unloaded += (_, _) =>
			{
				_holder.Remove(_source, this);
				_source = null;
			};
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

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			if (_holder.TryGetItem(_source, out Item item))
			{
				item.BrightnessProtruded = null; // Reset;
			}
		}

		private void OnMoved(object sender, (object source, double delta, bool update) e)
		{
			if (ReferenceEquals(this, sender) || ReferenceEquals(this._source, e.source))
				return;

			if (!_holder.TryGetItem(_source, out Item item) ||
				!ReferenceEquals(item.Sliders.FirstOrDefault(x => x.IsUnison), this))
				return;

			if (e.delta != 0D)
			{
				item.BrightnessProtruded ??= this.Value;
				item.BrightnessProtruded += e.delta * GetRangeRate();

				UpdateValue(item.BrightnessProtruded.Value);
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