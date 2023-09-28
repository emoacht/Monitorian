using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScreenFrame.Movers;

internal class WindowWatcher
{
	#region Win32

	[DllImport("User32.dll")]
	private static extern IntPtr SetWinEventHook(
		uint eventMin,
		uint eventMax,
		IntPtr hmodWinEventProc,
		WinEventProc pfnWinEventProc,
		uint idProcess,
		uint idThread,
		WINEVENT dwflags);

	private delegate void WinEventProc(
		IntPtr hWinEventHook,
		uint eventValue,
		IntPtr hwnd,
		int idObject,
		int idChild,
		uint dwEventThread,
		uint dwmsEventTime);

	[Flags]
	private enum WINEVENT : uint
	{
		WINEVENT_INCONTEXT = 4,
		WINEVENT_OUTOFCONTEXT = 0,
		WINEVENT_SKIPOWNPROCESS = 2,
		WINEVENT_SKIPOWNTHREAD = 1
	}

	[DllImport("User32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

	#endregion

	private readonly uint _eventValue;
	private readonly Action _onGenerated;

	public WindowWatcher(uint eventValue, Action onGenerated)
	{
		this._eventValue = eventValue;
		this._onGenerated = onGenerated ?? throw new ArgumentNullException(nameof(onGenerated));
	}

	private IntPtr _eventHook;
	private WinEventProc _eventProc;

	private readonly object _lock = new();

	public void AddHook()
	{
		lock (_lock)
		{
			if (_eventHook != IntPtr.Zero)
				return;

			_eventProc = EventProc; // Prevent delegate of non-static method from being gabage collected.
			_eventHook = SetWinEventHook(
				_eventValue,
				_eventValue,
				IntPtr.Zero,
				_eventProc,
				0,
				0,
				WINEVENT.WINEVENT_OUTOFCONTEXT | WINEVENT.WINEVENT_SKIPOWNPROCESS);
			if (_eventHook == IntPtr.Zero)
			{
				throw new Win32Exception("Failed to set an event hook function.");
			}
		}
	}

	public void RemoveHook()
	{
		lock (_lock)
		{
			if (_eventHook == IntPtr.Zero)
				return;

			if (UnhookWinEvent(_eventHook))
			{
				_eventHook = IntPtr.Zero;
			}
		}
	}

	private void EventProc(IntPtr hWinEventHook, uint eventValue, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		if (eventValue == _eventValue)
		{
			_onGenerated.Invoke();
		}
	}
}