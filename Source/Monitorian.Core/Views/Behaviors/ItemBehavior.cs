using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public interface IItemBehavior
{
	bool IsByKey { get; set; }
	ItemBehaviorGroup Group { get; }
}

public class ItemBehaviorGroup
{
	public bool Value { get; set; }
	public HashSet<IItemBehavior> Behaviors { get; } = new HashSet<IItemBehavior>();
}

public abstract class ItemBehavior<T> : Behavior<T>, IItemBehavior where T : DependencyObject
{
	public bool IsByKey
	{
		get { return (bool)GetValue(IsByKeyProperty); }
		set { SetValue(IsByKeyProperty, value); }
	}
	public static readonly DependencyProperty IsByKeyProperty =
		DependencyProperty.Register(
			"IsByKey",
			typeof(bool),
			typeof(ItemBehavior<T>),
			new PropertyMetadata(
				false,
				(d, e) => ((ItemBehavior<T>)d).Conform((bool)e.NewValue)));

	public ItemBehaviorGroup Group { get; private set; }

	protected virtual bool Subscribe(Type targetType)
	{
		var target = this.AssociatedObject.GetSelfAndAncestors()
			.FirstOrDefault(x =>
			{
				var type = x.GetType();
				return (type == targetType) || type.IsSubclassOf(targetType);
			});
		if (target is null)
			return false;

		var behavior = Interaction.GetBehaviors(target).OfType<IItemBehavior>().FirstOrDefault();
		if (behavior is null)
			return false;

		Group = behavior.Group ?? new ItemBehaviorGroup();
		return Group.Behaviors.Add(this);
	}

	protected virtual void Unsubscribe()
	{
		Group?.Behaviors.Remove(this);
		Group = null;
	}

	protected virtual void Conform(bool value)
	{
		if ((Group is null) || (Group.Value == value))
			return;

		Group.Value = value;

		foreach (var behavior in Group.Behaviors.Where(x => !ReferenceEquals(x, this)))
			behavior.IsByKey = value;
	}
}