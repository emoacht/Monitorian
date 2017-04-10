using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Properties;

namespace Monitorian.Models
{
	internal class ConsoleLogService
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

		public static void Start()
		{
			StartConsole();

			App.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		public static void End()
		{
			EndConsole();

			App.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		}

		private static ConsoleTraceListener _listener;

		[Conditional("DEBUG")]
		private static void StartConsole()
		{
			if (Debugger.IsAttached || _listener != null)
				return;

			if (!AttachConsole(ATTACH_PARENT_PROCESS))
				return;

			_listener = new ConsoleTraceListener();
			Trace.Listeners.Add(_listener);
		}

		[Conditional("DEBUG")]
		private static void EndConsole()
		{
			if (_listener == null)
				return;

			Trace.Listeners.Remove(_listener);

			FreeConsole();
		}

		private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			OnException(sender, e.Exception, "DispatcherUnhandledException");
			//e.Handled = true;
		}

		private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			OnException(sender, e.Exception, "UnobservedTaskException");
			//e.SetObserved();
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			OnException(sender, (Exception)e.ExceptionObject, "UnhandledException");
		}

		private static void OnException(object sender, Exception exception, string description)
		{
			if (Debugger.IsAttached || _listener != null)
			{
				Debug.WriteLine($"[{description}]" + Environment.NewLine
					+ exception);
			}
			else
			{
				Record(sender, exception);
			}
		}

		#region Record

		private const string LogFileName = "log.txt";

		/// <summary>
		/// Records log to AppData.
		/// </summary>
		/// <param name="log">Log</param>
		public static void Record(string log)
		{
			var content = $"[Date: {DateTime.Now}]" + Environment.NewLine
				+ log + Environment.NewLine + Environment.NewLine;

			RecordToAppData(content);
		}

		/// <summary>
		/// Records exception to AppData and Desktop.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="exception">Exception</param>
		/// <remarks>A log file of previous dates will be overridden.</remarks>
		public static void Record(object sender, Exception exception)
		{
			var content = $"[Date: {DateTime.Now} Sender: {sender}]" + Environment.NewLine
				+ exception + Environment.NewLine + Environment.NewLine;

			RecordToAppData(content);
			RecordToDesktop(content);
		}

		private static void RecordToAppData(string content)
		{
			try
			{
				var filePath = Path.Combine(
					FolderService.GetAppDataFolderPath(true),
					LogFileName);

				UpdateText(filePath, content);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to record exception log to AppData." + Environment.NewLine
					+ ex);
			}
		}

		private static void RecordToDesktop(string content)
		{
			var response = MessageBox.Show(
				Resources.RecordException,
				ProductInfo.Title,
				MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);
			if (response != MessageBoxResult.Yes)
				return;

			try
			{
				var filePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					LogFileName);

				UpdateText(filePath, content);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to record exception log to Desktop." + Environment.NewLine
					+ ex);
			}
		}

		private static void UpdateText(string filePath, string newContent)
		{
			string oldContent = null;

			if (File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
					oldContent = sr.ReadToEnd();
			}

			using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(string.Join(Environment.NewLine, GetLastLines(oldContent, "[Date:", 99).Reverse()) + newContent);
		}

		private static IEnumerable<string> GetLastLines(string source, string sectionHeader, int sectionCount)
		{
			if (string.IsNullOrEmpty(source))
				yield break;

			var lines = source.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			int count = 0;

			foreach (var line in lines.Reverse())
			{
				yield return line;

				if (!line.StartsWith(sectionHeader))
					continue;

				if (++count >= sectionCount)
					yield break;
			}
		}

		#endregion
	}
}