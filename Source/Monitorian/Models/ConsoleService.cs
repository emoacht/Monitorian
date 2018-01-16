using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models
{
	internal class ConsoleService
	{
		#region Win32

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AttachConsole(uint dwProcessId);

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		private const uint ATTACH_PARENT_PROCESS = uint.MaxValue;

		#endregion

		private static ConsoleTraceListener _listener;

		[Conditional("DEBUG")]
		public static void StartConsole()
		{
			if (Debugger.IsAttached || _listener != null)
				return;

			if (!AttachConsole(ATTACH_PARENT_PROCESS))
				return;

			_listener = new ConsoleTraceListener();
			Trace.Listeners.Add(_listener);
		}

		[Conditional("DEBUG")]
		public static void EndConsole()
		{
			if (_listener == null)
				return;

			Trace.Listeners.Remove(_listener);

			FreeConsole();
		}

		public static bool WriteConsole(Exception exception, string exceptionName)
		{
			if (Debugger.IsAttached || _listener != null)
			{
				Debug.WriteLine($"[{exceptionName}]" + Environment.NewLine
					+ exception);
				return true;
			}
			return false;
		}
	}
}