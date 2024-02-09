using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class UpdateSourceAction : TriggerAction<TextBox>
{
	protected override void Invoke(object parameter)
	{
		var textPropertyExpression = BindingOperations.GetBindingExpression(AssociatedObject, TextBox.TextProperty);
		if (!string.IsNullOrEmpty(textPropertyExpression?.ParentBinding?.Path.Path))
			textPropertyExpression.UpdateSource();
	}
}