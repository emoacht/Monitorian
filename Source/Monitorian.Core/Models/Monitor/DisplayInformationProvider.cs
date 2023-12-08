using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// A factory class for <see cref="Windows.Graphics.Display.DisplayInformation"/>
/// </summary>
/// <remarks>
/// <see cref="Windows.Graphics.Display.DisplayInformation"/> is only available for
/// .NET Framework on Windows 11 (10.0.22621.0) or greater. 
/// </remarks>
internal static class DisplayInformationProvider
{
	#region COM

	// From windows.graphics.display.interop.h
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
	[Guid("7449121C-382B-4705-8DA7-A795BA482013")]
	public interface IDisplayInformationStaticsInterop
	{
		[PreserveSig]
		uint GetForWindow(IntPtr window, [In] ref Guid riid, out Windows.Graphics.Display.DisplayInformation info);

		[PreserveSig]
		uint GetForMonitor(IntPtr monitor, [In] ref Guid riid, out Windows.Graphics.Display.DisplayInformation info);
	}

	private const uint S_OK = 0;

	#endregion

	#region Win32

	// From DispatcherQueue.h
	[DllImport("CoreMessaging.dll")]
	private static extern int CreateDispatcherQueueController(
		DispatcherQueueOptions options,
		ref Windows.System.DispatcherQueueController dispatcherQueueController);

	[StructLayout(LayoutKind.Sequential)]
	private struct DispatcherQueueOptions
	{
		public uint dwSize;
		public DISPATCHERQUEUE_THREAD_TYPE threadType;
		public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
	}

	private enum DISPATCHERQUEUE_THREAD_TYPE
	{
		DQTYPE_THREAD_DEDICATED = 1,
		DQTYPE_THREAD_CURRENT = 2,
	};

	private enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
	{
		DQTAT_COM_NONE = 0,
		DQTAT_COM_ASTA = 1,
		DQTAT_COM_STA = 2
	};

	#endregion

	#region Holder

	private class Holder
	{
		public readonly string DeviceInstanceId;
		public Windows.Graphics.Display.DisplayInformation DisplayInfo { get; private set; }
		public int Count = 1;
		public bool IsActive { get; private set; } = true; // default

		private Windows.Graphics.Display.AdvancedColorInfo _currentColorInfo;
		private readonly object _closeLock = new();

		public Holder(string deviceInstanceId, Windows.Graphics.Display.DisplayInformation displayInfo)
		{
			this.DeviceInstanceId = deviceInstanceId;
			this.DisplayInfo = displayInfo;
			_currentColorInfo = displayInfo.GetAdvancedColorInfo();

			// An event handler to DisplayInformation's events must be registered within
			// a callback of DispatcherQueueController.DispatcherQueue.TryEnqueue method.
			this.DisplayInfo.AdvancedColorInfoChanged += OnAdvancedColorInfoChanged;
		}

		private void OnAdvancedColorInfoChanged(Windows.Graphics.Display.DisplayInformation sender, object args)
		{
			lock (_closeLock)
			{
				var oldColorInfo = _currentColorInfo;
				_currentColorInfo = sender.GetAdvancedColorInfo();

				if (_currentColorInfo.CurrentAdvancedColorKind != oldColorInfo.CurrentAdvancedColorKind)
				{
					// It is observed that in the case of non-primary monitor, after AdvancedColorKind changes,
					// this event will no longer be fired by existing DisplayInformation. Thus it is necessary
					// to replace it with new one which is obtained after that change.
					// In addition, if an event handler is unregistered within a callback of
					// DispatcherQueueController.DispatcherQueue.TryEnqueue method, ArgumentException will be
					// thrown with a message: Delegate to an instance method cannot have null 'this'
					sender.AdvancedColorInfoChanged -= OnAdvancedColorInfoChanged;
					IsActive = false;
				}
			}

			DisplayInformationProvider.AdvancedColorInfoChanged?.Invoke(sender, DeviceInstanceId);
		}

		public void Replace(Windows.Graphics.Display.DisplayInformation displayInfo)
		{
			lock (_closeLock)
			{
				_currentColorInfo = displayInfo.GetAdvancedColorInfo();

				this.DisplayInfo = displayInfo;
				this.DisplayInfo.AdvancedColorInfoChanged += OnAdvancedColorInfoChanged;
				IsActive = true;
			}

			DisplayInformationProvider.AdvancedColorInfoChanged?.Invoke(displayInfo, DeviceInstanceId);
		}

		public void Close()
		{
			lock (_closeLock)
			{
				if (DisplayInfo is null)
					return;

				this.DisplayInfo.AdvancedColorInfoChanged -= OnAdvancedColorInfoChanged;
				DisplayInfo = null;
			}
		}
	}

	public static event EventHandler<string> AdvancedColorInfoChanged;

	private static readonly List<Holder> _holders = [];
	private static readonly object _registerLock = new();

	public static Action RegisterMonitor(string deviceInstanceId, IntPtr monitorHandle)
	{
		if (string.IsNullOrWhiteSpace(deviceInstanceId))
			throw new ArgumentNullException(nameof(deviceInstanceId));

		lock (_registerLock)
		{
			var holder = _holders.FirstOrDefault(x => x.DeviceInstanceId == deviceInstanceId);
			if (holder is not { IsActive: true })
			{
				_dispatcherQueueController.DispatcherQueue.TryEnqueue(() =>
				{
					var displayInfo = GetForMonitor(monitorHandle);
					if (displayInfo is not null)
					{
						if (holder is null)
						{
							_holders.Add(new Holder(deviceInstanceId, displayInfo));
						}
						else
						{
							holder.Replace(displayInfo);
							holder.Count++;
						}
					}
				});
			}
			else
			{
				holder.Count++;
			}
		}
		return new Action(() => UnregisterMonitor(deviceInstanceId));
	}

	private static void UnregisterMonitor(string deviceInstanceId)
	{
		lock (_registerLock)
		{
			int index = _holders.FindIndex(x => x.DeviceInstanceId == deviceInstanceId);
			if (index < 0)
				return;

			if (--_holders[index].Count > 0)
				return;

			_holders[index].Close();
			_holders.RemoveAt(index);
		}
	}

	public static void ClearMonitors()
	{
		foreach (var h in _holders)
			h.Close();

		_holders.Clear();
	}

	public static Windows.Graphics.Display.AdvancedColorInfo GetAdvancedColorInfo(string deviceInstanceId)
	{
		return _holders.FirstOrDefault(x => x.DeviceInstanceId == deviceInstanceId)?.DisplayInfo?.GetAdvancedColorInfo();
	}

	#endregion

	/// <summary>
	/// Gets DisplayInformation for a specified window.
	/// </summary>
	/// <param name="windowHandle">Window handle</param>
	/// <returns>DisplayInformation if successfully gets. Null otherwise.</returns>
	/// <remarks>This method must be called when Windows.System.DispatcherQueue is running.</remarks>
	public static Windows.Graphics.Display.DisplayInformation GetForWindow(IntPtr windowHandle)
	{
		if (_dispatcherQueueController is null)
			return null;

		var factory = (IDisplayInformationStaticsInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(Windows.Graphics.Display.DisplayInformation));
		var iid = typeof(Windows.Graphics.Display.DisplayInformation).GetInterface("IDisplayInformation").GUID;
		var result = factory.GetForWindow(windowHandle, ref iid, out Windows.Graphics.Display.DisplayInformation displayInfo);
		return (result == S_OK)
			? displayInfo
			: null;
	}

	/// <summary>
	/// Gets DisplayInformation for a specified monitor.
	/// </summary>
	/// <param name="monitorHandle">Monitor handle</param>
	/// <returns>DisplayInformation if successfully gets. Null otherwise.</returns>
	public static Windows.Graphics.Display.DisplayInformation GetForMonitor(IntPtr monitorHandle)
	{
		var factory = (IDisplayInformationStaticsInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(Windows.Graphics.Display.DisplayInformation));
		var iid = typeof(Windows.Graphics.Display.DisplayInformation).GetInterface("IDisplayInformation").GUID;
		var result = factory.GetForMonitor(monitorHandle, ref iid, out Windows.Graphics.Display.DisplayInformation displayInfo);
		return (result == S_OK)
			? displayInfo
			: null;
	}

	private static Windows.System.DispatcherQueueController _dispatcherQueueController = null;

	/// <summary>
	/// Ensures that <see cref="Windows.System.DispatcherQueue"/> is running.
	/// </summary>
	/// <remarks>
	/// This method must be called before registering an event handler to DisplayInformation's events.
	/// </remarks>
	public static void EnsureDispatcherQueue()
	{
		if ((Windows.System.DispatcherQueue.GetForCurrentThread() is not null) ||
			(_dispatcherQueueController is not null))
			return;

		var options = new DispatcherQueueOptions
		{
			dwSize = (uint)Marshal.SizeOf<DispatcherQueueOptions>(),
			threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,
			apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA
		};
		CreateDispatcherQueueController(options, ref _dispatcherQueueController);
	}
}