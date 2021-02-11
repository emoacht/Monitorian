using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors
{
	public class ManipulationCallMethodAction : TriggerAction<DependencyObject>
	{
		public object TargetObject
		{
			get { return (object)GetValue(TargetObjectProperty); }
			set { SetValue(TargetObjectProperty, value); }
		}
		public static readonly DependencyProperty TargetObjectProperty =
			DependencyProperty.Register(
				"TargetObject",
				typeof(object),
				typeof(ManipulationCallMethodAction),
				new PropertyMetadata(
					null,
					(d, e) => ((ManipulationCallMethodAction)d).SetMethods()));

		public string IncreaseMethodName { get; set; }
		public string DecreaseMethodName { get; set; }

		private MethodInfo _increaseMethod;
		private MethodInfo _decreaseMethod;

		private void SetMethods()
		{
			if (TargetObject is null)
				return;

			var targetType = TargetObject.GetType();

			if (!string.IsNullOrEmpty(IncreaseMethodName))
				_increaseMethod = targetType.GetMethod(IncreaseMethodName, Type.EmptyTypes);

			if (!string.IsNullOrEmpty(DecreaseMethodName))
				_decreaseMethod = targetType.GetMethod(DecreaseMethodName, Type.EmptyTypes);
		}

		protected override void Invoke(object parameter)
		{
			if (parameter is not ManipulationCompletedEventArgs e)
				throw new InvalidOperationException("This action must be called by ManipulationCompleted event.");

			switch (e.TotalManipulation.Translation.X) // Horizontal motion
			{
				case > 0D:
					_increaseMethod?.Invoke(TargetObject, null);
					break;
				case < 0D:
					_decreaseMethod?.Invoke(TargetObject, null);
					break;
			}
		}
	}
}