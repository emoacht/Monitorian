using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Monitorian.Core.Models;

internal class ConsoleService
{
	#region Win32

	[DllImport("Kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AttachConsole(uint dwProcessId);

	private const uint ATTACH_PARENT_PROCESS = unchecked((uint)-1);

	[DllImport("Kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool FreeConsole();

	[DllImport("Kernel32.dll", SetLastError = true)]
	private static extern IntPtr GetStdHandle(uint nStdHandle);

	private const uint STD_OUTPUT_HANDLE = unchecked((uint)-11);

	private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

	[DllImport("Kernel32.dll", SetLastError = true)]
	private static extern FILE_TYPE GetFileType(IntPtr hFile);

	private enum FILE_TYPE : uint
	{
		FILE_TYPE_UNKNOWN = 0x0000,
		FILE_TYPE_DISK = 0x0001,
		FILE_TYPE_CHAR = 0x0002,
		FILE_TYPE_PIPE = 0x0003
	}

	[DllImport("Kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetConsoleMode(
		IntPtr hConsoleHandle,
		out int lpMode);

	#endregion

	/// <summary>
	/// Determines whether output is redirected from the standard output stream.
	/// </summary>
	/// <returns>True if output is redirected</returns>
	/// <remarks>
	/// This method substitutes <see cref="System.Console.IsOutputRedirected"/> property which
	/// cannot correctly handle a case where output handle cannot be obtained. It happens when
	/// output has not been redirected.
	/// </remarks>
	private static bool IsOutputRedirected()
	{
		var outputHandle = GetStdHandle(STD_OUTPUT_HANDLE);
		if ((outputHandle == IntPtr.Zero) || (outputHandle == INVALID_HANDLE_VALUE))
			return false;

		return GetFileType(outputHandle) switch
		{
			FILE_TYPE.FILE_TYPE_DISK or
			FILE_TYPE.FILE_TYPE_PIPE => true,
			FILE_TYPE.FILE_TYPE_CHAR => !GetConsoleMode(outputHandle, out _),
			_ => false
		};
	}

	private static ConsoleTraceListener _listener;

	/// <summary>
	/// Attempts to start writing to standard output by attaching to the console.
	/// </summary>
	/// <returns>True if successfully starts</returns>
	public static bool TryStartWrite()
	{
		if (Debugger.IsAttached || (_listener is not null))
			return false;

		if (IsOutputRedirected())
			Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

		if (!AttachConsole(ATTACH_PARENT_PROCESS))
			return false;

		_listener = new ConsoleTraceListener();
		Trace.Listeners.Add(_listener);
		return true;
	}

	/// <summary>
	/// Ends writing to standard output by detaching from the console.
	/// </summary>
	public static void EndWrite()
	{
		if (_listener is null)
			return;

		Trace.Listeners.Remove(_listener);
		_listener.Dispose();
		_listener = null;

		FreeConsole();
	}

	public static bool WriteLine(Exception exception, string exceptionName)
	{
		var content = $"[{exceptionName}]" + Environment.NewLine
			+ exception;

		return WriteLine(content);
	}

	public static bool WriteLine(string content)
	{
		if (Debugger.IsAttached || (_listener is not null))
		{
			Trace.WriteLine(content);
			return true;
		}
		return false;
	}
}