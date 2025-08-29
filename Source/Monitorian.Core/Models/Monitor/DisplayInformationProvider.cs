using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;

using Monitorian.Core.Helper;

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

	// Derived from windows.graphics.display.interop.h
	// https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.display.interop/nn-windows-graphics-display-interop-idisplayinformationstaticsinterop
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

	// Derived from DispatcherQueue.h
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

	#region Type

	[DataContract]
	public class DisplayItem
	{
		[DataMember(Order = 0)]
		public bool IsHighDynamicRangeSupported { get; private set; }

		[DataMember(Order = 1)]
		public string AdvancedColorKind { get; private set; }

		[DataMember(Order = 2)]
		public string SdrWhiteLevel { get; private set; }

		[DataMember(Order = 3)]
		public string MinLuminance { get; private set; }

		[DataMember(Order = 4)]
		public string MaxLuminance { get; private set; }

		public DisplayItem(IntPtr monitorHandle)
		{
			if (!OsVersion.Is11Build22621OrGreater)
				return;

			var displayInfo = GetForMonitor(monitorHandle);
			if (displayInfo is null)
				return;

			var aci = displayInfo.GetAdvancedColorInfo();
			IsHighDynamicRangeSupported = aci.IsAdvancedColorKindAvailable(Windows.Graphics.Display.AdvancedColorKind.HighDynamicRange);
			AdvancedColorKind = aci.CurrentAdvancedColorKind.ToString();
			SdrWhiteLevel = $"{aci.SdrWhiteLevelInNits} nits";
			MinLuminance = $"{aci.MinLuminanceInNits} nits";
			MaxLuminance = $"{aci.MaxLuminanceInNits} nits";
		}
	}

	#endregion

	#region Holder

	private class Holder
	{
		public readonly string DeviceInstanceId;
		public Windows.Graphics.Display.DisplayInformation DisplayInfo { get; private set; }
		public int Count { get; set; } = 1;
		public bool IsActive { get; private set; } = true; // default

		private Windows.Graphics.Display.AdvancedColorKind _currentAdvancedColorKind;
		private readonly object _closeLock = new();

		public Holder(string deviceInstanceId, Windows.Graphics.Display.DisplayInformation displayInfo)
		{
			this.DeviceInstanceId = deviceInstanceId;
			this.DisplayInfo = displayInfo;
			_currentAdvancedColorKind = displayInfo.GetAdvancedColorInfo().CurrentAdvancedColorKind;

			// An event handler to DisplayInformation's events must be registered within
			// a callback of DispatcherQueueController.DispatcherQueue.TryEnqueue method.
			this.DisplayInfo.AdvancedColorInfoChanged += OnAdvancedColorInfoChanged;
		}

		private void OnAdvancedColorInfoChanged(Windows.Graphics.Display.DisplayInformation sender, object args)
		{
			float sdrWhiteLevel = -1;

			lock (_closeLock)
			{
				var aci = sender.GetAdvancedColorInfo();
				if (aci.CurrentAdvancedColorKind is Windows.Graphics.Display.AdvancedColorKind.HighDynamicRange)
					sdrWhiteLevel = aci.SdrWhiteLevelInNits;

				var oldAdvancedColorKind = _currentAdvancedColorKind;
				_currentAdvancedColorKind = aci.CurrentAdvancedColorKind;

				if (_currentAdvancedColorKind != oldAdvancedColorKind)
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

			DisplayInformationProvider.AdvancedColorInfoChanged?.Invoke(sender, (DeviceInstanceId, sdrWhiteLevel));
		}

		public void Replace(Windows.Graphics.Display.DisplayInformation displayInfo)
		{
			float sdrWhiteLevel = -1;

			lock (_closeLock)
			{
				this.DisplayInfo = displayInfo;
				var aci = displayInfo.GetAdvancedColorInfo();
				if (aci.CurrentAdvancedColorKind is Windows.Graphics.Display.AdvancedColorKind.HighDynamicRange)
					sdrWhiteLevel = aci.SdrWhiteLevelInNits;

				_currentAdvancedColorKind = aci.CurrentAdvancedColorKind;
				this.DisplayInfo.AdvancedColorInfoChanged += OnAdvancedColorInfoChanged;
				IsActive = true;
			}

			DisplayInformationProvider.AdvancedColorInfoChanged?.Invoke(displayInfo, (DeviceInstanceId, sdrWhiteLevel));
		}

		public void Close()
		{
			lock (_closeLock)
			{
				if (DisplayInfo is null)
					return;

				DisplayInfo.AdvancedColorInfoChanged -= OnAdvancedColorInfoChanged;
				DisplayInfo = null;
			}
		}
	}

	public static event EventHandler<(string deviceInstanceId, float sdrWhiteLevel)> AdvancedColorInfoChanged;

	private static readonly Dictionary<string, Holder> _holders = [];
	private static readonly object _registerLock = new();

	public static void RegisterMonitor(string deviceInstanceId, IntPtr monitorHandle)
	{
		lock (_registerLock)
		{
			_holders.TryGetValue(deviceInstanceId, out Holder holder);
			if (holder is not { IsActive: true })
			{
				_dispatcherQueueController.DispatcherQueue.TryEnqueue(() =>
				{
					var displayInfo = GetForMonitor(monitorHandle);
					if (displayInfo is not null)
					{
						if (holder is null)
						{
							_holders[deviceInstanceId] = new Holder(deviceInstanceId, displayInfo);
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
	}

	public static void UnregisterMonitor(string deviceInstanceId)
	{
		lock (_registerLock)
		{
			if (!_holders.TryGetValue(deviceInstanceId, out Holder holder))
				return;

			if (--holder.Count > 0)
				return;

			holder.Close();
			_holders.Remove(deviceInstanceId);
		}
	}

	public static void ClearMonitors()
	{
		foreach (var h in _holders.Values)
			h.Close();

		_holders.Clear();
	}

	public static (AccessResult result, float sdrWhiteLevel) GetSdrWhiteLevel(string deviceInstanceId)
	{
		if (!_holders.TryGetValue(deviceInstanceId, out Holder holder))
			return (new AccessResult(AccessStatus.Failed, "The monitor has not been registered yet."), -1);

		var aci = holder.DisplayInfo.GetAdvancedColorInfo();
		if (aci.CurrentAdvancedColorKind is not Windows.Graphics.Display.AdvancedColorKind.HighDynamicRange)
			return (AccessResult.NotSupported, -1);

		return (AccessResult.Succeeded, aci.SdrWhiteLevelInNits);
	}

	#endregion

	/// <summary>
	/// Determines if HDR is set for a specified monitor and if so, gets SDR white level
	/// (10.0.22621.0 or greater only).
	/// </summary>
	/// <param name="monitorHandle">Monitor handle</param>
	/// <returns>
	/// <para>isHdr: True if successfully determines that HDR is set</para>
	/// <para>sdrWhiteLevel: SDR white level if HDR is set</para>
	/// </returns>
	public static (bool isHdr, float sdrWhiteLevel) IsHdrAndGetSdrWhiteLevel(IntPtr monitorHandle)
	{
		if (!OsVersion.Is11Build22621OrGreater)
			return (false, -1);

		var displayInfo = GetForMonitor(monitorHandle);

		var aci = displayInfo?.GetAdvancedColorInfo();
		if (aci is not { CurrentAdvancedColorKind: Windows.Graphics.Display.AdvancedColorKind.HighDynamicRange })
			return (false, -1);

		return (isHdr: true, aci.SdrWhiteLevelInNits);
	}

	/// <summary>
	/// Gets DisplayInformation for a specified window.
	/// </summary>
	/// <param name="windowHandle">Window handle</param>
	/// <returns>DisplayInformation if successfully gets. Null otherwise.</returns>
	/// <remarks>This method must be called when Windows.System.DispatcherQueue is running.</remarks>
	private static Windows.Graphics.Display.DisplayInformation GetForWindow(IntPtr windowHandle)
	{
		if (_dispatcherQueueController is null)
			return null;

		var factory = (IDisplayInformationStaticsInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(Windows.Graphics.Display.DisplayInformation));
		var iid = typeof(Windows.Graphics.Display.DisplayInformation).GetInterface("IDisplayInformation").GUID;
		var result = factory.GetForWindow(windowHandle, ref iid, out Windows.Graphics.Display.DisplayInformation displayInfo);
		return (result is S_OK)
			? displayInfo
			: null;
	}

	/// <summary>
	/// Gets DisplayInformation for a specified monitor.
	/// </summary>
	/// <param name="monitorHandle">Monitor handle</param>
	/// <returns>DisplayInformation if successfully gets. Null otherwise.</returns>
	private static Windows.Graphics.Display.DisplayInformation GetForMonitor(IntPtr monitorHandle)
	{
		var factory = (IDisplayInformationStaticsInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(Windows.Graphics.Display.DisplayInformation));
		var iid = typeof(Windows.Graphics.Display.DisplayInformation).GetInterface("IDisplayInformation").GUID;
		var result = factory.GetForMonitor(monitorHandle, ref iid, out Windows.Graphics.Display.DisplayInformation displayInfo);
		return (result is S_OK)
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
		if (!OsVersion.Is11Build22621OrGreater ||
			(_dispatcherQueueController is not null) ||
			(Windows.System.DispatcherQueue.GetForCurrentThread() is not null))
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