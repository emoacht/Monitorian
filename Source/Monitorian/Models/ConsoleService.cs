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
		public static extern bool AttachConsole(uint dwProcessId);

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		#endregion

		private static ConsoleTraceListener _listener;

		[Conditional("DEBUG")]
		public static void StartAllocConsole()
		{
			if (Debugger.IsAttached || _listener != null)
				return;

			AllocConsole();

			_listener = new ConsoleTraceListener();
			Trace.Listeners.Add(_listener);
		}

		[Conditional("DEBUG")]
		public static void StartAttachConsole()
		{
			if (Debugger.IsAttached || _listener != null)
				return;

			AttachConsole(uint.MaxValue);

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
	}
}