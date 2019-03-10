using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

using Monitorian.Models.Monitor;
using Monitorian.Helper;

namespace Monitorian
{
	class HotKeyListener : IDisposable
	{
		public enum HotKeyId : int
		{
			IncrementBrightness,
			DecrementBrightness
		}

		private readonly HotKeyForm _listenerForm;

		/// <summary>
		/// Occurs when a hot key registerd by the RegisterHotKey function is pressed.
		/// </summary>
		public event HotKeyEventHandler HotKeyDown;

		public HotKeyListener()
		{
			var requests = new List<HotKeyTuple>();
			requests.Add(new HotKeyTuple
			{
				Id = (int)HotKeyId.IncrementBrightness,
				KeyModifiers = HotKeyHelper.KeyModifiers.MOD_WIN,
				VirtualKey = Keys.F9
			});
			requests.Add(new HotKeyTuple
			{
				Id = (int)HotKeyId.DecrementBrightness,
				KeyModifiers = HotKeyHelper.KeyModifiers.MOD_WIN,
				VirtualKey = Keys.F8
			});

			_listenerForm = new HotKeyForm(requests, this);
		}

		#region IDisposable

		private bool _isDisposed = false;

		/// <summary>
		/// Public implementation of Dispose pattern
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Protected implementation of Dispose pattern
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				_listenerForm?.Dispose();
			}

			_isDisposed = true;
		}

		#endregion

		private class HotKeyForm : Form
		{
			private List<HotKeyTuple> _hotKeys;
			private HotKeyListener _ownerListener;

			public HotKeyForm(List<HotKeyTuple> hotKeys, HotKeyListener ownerListener)
			{
				_hotKeys = hotKeys;
				_ownerListener = ownerListener;

				foreach (var hotKey in hotKeys)
				{
					var result = HotKeyHelper.RegisterHotKey(this.Handle, hotKey.Id, hotKey.KeyModifiers, hotKey.VirtualKey);
					if (!result)
					{
						Debug.WriteLine($"Failed to register the hot key. {Error.CreateMessage()}");
					}
				}
			}

			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
					case HotKeyHelper.WM_HOTKEY:
						var hotkeyTuple = new HotKeyTuple
						{
							Id = (int)m.WParam,
							VirtualKey = (Keys)Enum.ToObject(typeof(Keys), unchecked((short)(uint)m.WParam >> 16)),
							KeyModifiers = (HotKeyHelper.KeyModifiers)Enum.ToObject(typeof(HotKeyHelper.KeyModifiers), unchecked((short)m.WParam))
						};
						_ownerListener.HotKeyDown?.Invoke(this, new HotKeyEventArgs(hotkeyTuple));
						break;
				}

				base.WndProc(ref m);
			}

			protected override void Dispose(bool disposing)
			{
				foreach (var hotKey in _hotKeys)
				{
					var result = HotKeyHelper.UnregisterHotKey(this.Handle, hotKey.Id);
					if (!result)
					{
						Debug.WriteLine($"Failed to unregister the hot key. {Error.CreateMessage()}");
					}
				}
			}
		}
	}
}
