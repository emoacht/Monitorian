using System.Windows;
using System.Windows.Controls;

namespace Monitorian.Core.Views.Controls;

[TemplateVisualState(Name = nameof(PulseStates.Off), GroupName = nameof(PulseStates))]
[TemplateVisualState(Name = nameof(PulseStates.On), GroupName = nameof(PulseStates))]
public class PulseLabel : Label
{
	protected enum PulseStates
	{
		Off,
		On
	}

	#region Property

	public bool IsPulsing
	{
		get { return (bool)GetValue(IsPulsingProperty); }
		set { SetValue(IsPulsingProperty, value); }
	}
	public static readonly DependencyProperty IsPulsingProperty =
		DependencyProperty.Register(
			"IsPulsing",
			typeof(bool),
			typeof(PulseLabel),
			new PropertyMetadata(
				false,
				(d, e) => ((PulseLabel)d).ChangeVisualState(true)));

	#endregion

	protected virtual void ChangeVisualState(bool useTransitions)
	{
		if (!IsPulsing)
		{
			VisualStateManager.GoToState(this, nameof(PulseStates.Off), useTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, nameof(PulseStates.On), useTransitions);
		}
	}
}