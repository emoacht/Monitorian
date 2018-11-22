using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Helper;
using Monitorian.Properties;

namespace Monitorian.Models
{
	internal class LogService
	{
		private const string ProbeFileName = "probe.log";
		private const string OperationFileName = "operation.log";
		private const string ExceptionFileName = "exception.log";

		private const string HeaderStart = "[Date:";
		private static string ComposeHeader() => $"{HeaderStart} {DateTime.Now} Ver: {ProductInfo.Version}]";

		/// <summary>
		/// Records probe log to Desktop.
		/// </summary>
		/// <param name="log">Log</param>
		/// <remarks>A log file will be overridden.</remarks>
		public static void RecordProbe(string log)
		{
			var content = ComposeHeader() + Environment.NewLine
				+ log;

			if (MessageBox.Show(
				Resources.RecordProbe,
				ProductInfo.Title,
				MessageBoxButton.OKCancel,
				MessageBoxImage.Information,
				MessageBoxResult.OK) != MessageBoxResult.OK)
				return;

			RecordToDesktop(ProbeFileName, content, false);
		}

		/// <summary>
		/// Records operation log to AppData.
		/// </summary>
		/// <param name="log">Log</param>
		/// <remarks>A log file of previous dates will be overridden.</remarks>
		public static void RecordOperation(string log)
		{
			var content = ComposeHeader() + Environment.NewLine
				+ log + Environment.NewLine + Environment.NewLine;

			RecordToAppData(OperationFileName, content);
		}

		/// <summary>
		/// Records exception log to AppData and Desktop.
		/// </summary>
		/// <param name="fileName">File name</param>
		/// <param name="exception">Exception</param>
		/// <remarks>A log file of previous dates will be overridden.</remarks>
		public static void RecordException(Exception exception)
		{
			var content = ComposeHeader() + Environment.NewLine
				+ exception.ToDetailedString() + Environment.NewLine + Environment.NewLine;

			RecordToAppData(ExceptionFileName, content);

			if (MessageBox.Show(
				Resources.RecordException,
				ProductInfo.Title,
				MessageBoxButton.YesNo,
				MessageBoxImage.Error,
				MessageBoxResult.Yes) != MessageBoxResult.Yes)
				return;

			RecordToDesktop(ExceptionFileName, content, true);
		}

		#region Helper

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

		private static void RecordToDesktop(string fileName, string content, bool update)
		{
			try
			{
				var desktopFilePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					fileName);

				if (update)
				{
					UpdateText(desktopFilePath, content);
				}
				else
				{
					SaveText(desktopFilePath, content);
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to Desktop." + Environment.NewLine
					+ ex);
			}
		}

		private static void SaveText(string filePath, string content)
		{
			using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(content);
		}

		private const int MaxSectionCount = 100;

		private static void UpdateText(string filePath, string newContent)
		{
			string oldContent = null;

			if (File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
					oldContent = sr.ReadToEnd();

				oldContent = string.Join(Environment.NewLine, EnumerateLastLines(oldContent, HeaderStart, MaxSectionCount - 1).Reverse());
			}

			SaveText(filePath, oldContent + newContent);
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