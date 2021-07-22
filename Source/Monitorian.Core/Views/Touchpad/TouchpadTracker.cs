using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Views.Touchpad
{
	public class TouchpadTracker
	{
		private readonly Window _window;

		public TouchpadTracker(Window window)
		{
			if (!TouchpadHelper.Exists())
				return;

			this._window = window ?? throw new ArgumentNullException(nameof(window));
			this._window.SourceInitialized += OnSourceInitialized;
			this._window.Closed += OnClosed;
		}

		private Throttle _complete;
		private HwndSource _source;

		private void OnSourceInitialized(object sender, EventArgs e)
		{
			_complete = new Throttle(TimeSpan.FromMilliseconds(100), Complete);

			_source = (HwndSource)PresentationSource.FromVisual(_window);
			_source.AddHook(WndProc);

			TouchpadHelper.RegisterInput(_source.Handle);
		}

		private void OnClosed(object sender, EventArgs e)
		{
			ManipulationDelta = null;
			ManipulationCompleted = null;

			_source?.RemoveHook(WndProc);

			TouchpadHelper.UnregisterInput();
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case TouchpadHelper.WM_INPUT:
					var contacts = TouchpadHelper.ParseInput(lParam);
					if (contacts?.Length > 1)
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
			if (delta == 0)
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