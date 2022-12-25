using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Views.Input.Touchpad
{
	public class TouchpadTracker
	{
		private static readonly Dictionary<Window, TouchpadTracker> _windows = new();

		public static TouchpadTracker Create(UIElement element)
		{
			var window = Window.GetWindow(element);
			if (!_windows.TryGetValue(window, out TouchpadTracker tracker))
			{
				tracker = new TouchpadTracker(window);
			}
			return tracker;
		}

		private readonly Window _window;
		private readonly Throttle _complete;

		public TouchpadTracker(Window window)
		{
			if (!TouchpadHelper.Exists())
				return;

			this._window = window ?? throw new ArgumentNullException(nameof(window));
			_complete = new Throttle(TimeSpan.FromMilliseconds(100), Complete);

			_windows.Add(this._window, this);

			if (this._window.IsInitialized)
			{
				Register();
			}
			else
			{
				this._window.SourceInitialized += OnSourceInitialized;
			}
			this._window.Closed += OnClosed;

			void OnSourceInitialized(object sender, EventArgs e)
			{
				Register();
			}

			void OnClosed(object sender, EventArgs e)
			{
				Unregister();

				_windows.Remove(this._window);
			}
		}

		private HwndSource _source;

		private void Register()
		{
			_source = (HwndSource)PresentationSource.FromVisual(_window);
			_source.AddHook(WndProc);
			TouchpadHelper.RegisterInput(_source.Handle);
		}

		private void Unregister()
		{
			ManipulationDelta = null;
			ManipulationCompleted = null;

			TouchpadHelper.UnregisterInput();
			_source?.RemoveHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case TouchpadHelper.WM_INPUT:
					LastInputTimeStamp = Environment.TickCount;

					if ((ManipulationDelta is null) &&
						(ManipulationCompleted is null))
						break;

					var contacts = TouchpadHelper.ParseInput(lParam);
					if (contacts is { Length: > 1 })
					{
						Check(contacts[0]);
						handled = true;
					}
					break;
			}
			return IntPtr.Zero;
		}

		public event EventHandler<int> ManipulationDelta;
		public event EventHandler ManipulationCompleted;

		/// <summary>
		/// The number of milliseconds when last input event by touchpad occured
		/// </summary>
		public int LastInputTimeStamp { get; private set; }

		public int UnitResolution
		{
			get => _unitResolution;
			set => _unitResolution = Math.Max(1, value);
		}
		private int _unitResolution = 30; // Default

		private TouchpadContact _contact;

		private async void Check(TouchpadContact contact)
		{
			if ((_contact == default) ||
				(_contact.ContactId != contact.ContactId))
			{
				_contact = contact;
				return;
			}

			var vector = contact.Point - _contact.Point;
			var delta = (vector.X / UnitResolution) switch
			{
				>= 1 => 1,
				<= -1 => -1,
				_ => 0
			};
			if (delta is 0)
				return;

			_contact = contact;
			ManipulationDelta?.Invoke(_window, delta);

			await _complete.PushAsync();
		}

		private void Complete()
		{
			_contact = default;
			ManipulationCompleted?.Invoke(_window, EventArgs.Empty);
		}
	}
}