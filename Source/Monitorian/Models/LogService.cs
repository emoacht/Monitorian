using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Properties;

namespace Monitorian.Models
{
	internal class LogService
	{
		public static void Start()
		{
			ConsoleService.StartConsole();

			App.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		public static void End()
		{
			ConsoleService.EndConsole();

			App.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		}

		private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			OnException(sender, e.Exception, nameof(Application.DispatcherUnhandledException));
			//e.Handled = true;
		}

		private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			OnException(sender, e.Exception, nameof(TaskScheduler.UnobservedTaskException));
			//e.SetObserved();
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			OnException(sender, (Exception)e.ExceptionObject, nameof(AppDomain.UnhandledException));
		}

		private static void OnException(object sender, Exception exception, string exceptionName)
		{
			if (ConsoleService.WriteConsole(exception, exceptionName))
				return;

			RecordException(sender, exception);
		}

		#region Record

		private const string OperationFileName = "operation.log";
		private const string ExceptionFileName = "exception.log";

		/// <summary>
		/// Records operation to AppData.
		/// </summary>
		/// <param name="log">Log</param>
		public static void RecordOperation(string log)
		{
			var content = $"[Date: {DateTime.Now}]" + Environment.NewLine
				+ log + Environment.NewLine + Environment.NewLine;

			RecordToAppData(OperationFileName, content);
		}

		/// <summary>
		/// Records exception to AppData and Desktop.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="exception">Exception</param>
		/// <remarks>A log file of previous dates will be overridden.</remarks>
		public static void RecordException(object sender, Exception exception)
		{
			var content = $"[Date: {DateTime.Now} Sender: {sender}]" + Environment.NewLine
				+ exception + Environment.NewLine + Environment.NewLine;

			RecordToAppData(ExceptionFileName, content);
			RecordToDesktop(ExceptionFileName, content);
		}

		private static void RecordToAppData(string fileName, string content)
		{
			try
			{
				FolderService.AssureAppDataFolder();

				var appDataFilePath = Path.Combine(
					FolderService.AppDataFolderPath,
					fileName);

				UpdateText(appDataFilePath, content);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to AppData." + Environment.NewLine
					+ ex);
			}
		}

		private static string RecordMessage => Resources.RecordException;

		private static void RecordToDesktop(string fileName, string content)
		{
			var response = MessageBox.Show(
				RecordMessage,
				ProductInfo.Title,
				MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);
			if (response != MessageBoxResult.Yes)
				return;

			try
			{
				var desktopFilePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					fileName);

				UpdateText(desktopFilePath, content);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to Desktop." + Environment.NewLine
					+ ex);
			}
		}

		private const int MaxSectionCount = 100;

		private static void UpdateText(string filePath, string newContent)
		{
			string oldContent = null;

			if (File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
					oldContent = sr.ReadToEnd();

				oldContent = string.Join(Environment.NewLine, EnumerateLastLines(oldContent, "[Date:", MaxSectionCount - 1).Reverse());
			}

			using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(oldContent + newContent);
		}

		private static IEnumerable<string> EnumerateLastLines(string source, string sectionHeader, int sectionCount)
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