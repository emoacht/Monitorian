using System;
using System.Runtime.CompilerServices;

using Monitorian.Core.Models;
using Monitorian.Core.Views;

namespace Monitorian.Core.ViewModels;

public class ScrollWindowViewModel : ViewModelBase
{
	private readonly AppControllerCore _controller;
	public SettingsCore Settings => _controller.Settings;

	public ScrollWindowViewModel(AppControllerCore controller)
	{
		this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
	}

	#region Scroll

	public bool InvertsMouseVerticalWheel
	{
		get => HasFlag(ScrollInput.MouseVerticalWheel);
		set => ChangeFlag(ScrollInput.MouseVerticalWheel, value);
	}

	public bool InvertsMouseHorizontalWheel
	{
		get => HasFlag(ScrollInput.MouseHorizontalWheel);
		set => ChangeFlag(ScrollInput.MouseHorizontalWheel, value);
	}

	public bool InvertsTouchpadVerticalSwipe
	{
		get => HasFlag(ScrollInput.TouchpadVerticalSwipe);
		set => ChangeFlag(ScrollInput.TouchpadVerticalSwipe, value);
	}

	public bool InvertsTouchpadHorizontalSwipe
	{
		get => HasFlag(ScrollInput.TouchpadHorizontalSwipe);
		set => ChangeFlag(ScrollInput.TouchpadHorizontalSwipe, value);
	}

	private readonly object _lock = new();

	private bool HasFlag(ScrollInput flag)
	{
		lock (_lock)
		{
			return Settings.InvertsScrollDirection.HasFlag(flag);
		}
	}

	private void ChangeFlag(ScrollInput flag, bool value, [CallerMemberName] string propertyName = null)
	{
		lock (_lock)
		{
			if (value)
			{
				Settings.InvertsScrollDirection |= flag;
			}
			else
			{
				Settings.InvertsScrollDirection &= ~flag;
			}
		}
		OnPropertyChanged(propertyName);
	}

	#endregion
}