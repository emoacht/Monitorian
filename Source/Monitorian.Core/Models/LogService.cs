using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Core.Helper;
using Monitorian.Core.Properties;

namespace Monitorian.Core.Models
{
	public class LogService
	{
		private const string ProbeFileName = "probe.log";
		private const string OperationFileName = "operation.log";
		private const string ExceptionFileName = "exception.log";

		private const string HeaderStart = "[Date:";
		private static string ComposeHeader() => $"{HeaderStart} {DateTime.Now} Ver: {ProductInfo.Version}]";

		/// <summary>
		/// Records probe log to Desktop.
		/// </summary>
		/// <param name="content">Content</param>
		/// <remarks>
		/// The log file will be always overwritten.
		/// </remarks>
		public static void RecordProbe(string content)
		{
			content = ComposeHeader() + Environment.NewLine
				+ content;

			if (MessageBox.Show(
				Resources.RecordProbeMessage,
				ProductInfo.Title,
				MessageBoxButton.OKCancel,
				MessageBoxImage.Information,
				MessageBoxResult.OK) != MessageBoxResult.OK)
				return;

			RecordToDesktop(ProbeFileName, content);
		}

		/// <summary>
		/// Records operation log to AppData.
		/// </summary>
		/// <param name="content">Content</param>
		/// <remarks>
		/// The log file will be appended with new content as long as one day has not yet passed
		/// since last write. Otherwise, the log file will be overwritten.
		/// </remarks>
		public static void RecordOperation(string content)
		{
			content = ComposeHeader() + Environment.NewLine
				+ content + Environment.NewLine + Environment.NewLine;

			RecordToAppData(OperationFileName, content, 100);
		}

		/// <summary>
		/// Copies operation log to Desktop.
		/// </summary>
		public static void CopyOperation()
		{
			if (!TryReadFromAppData(OperationFileName, out string content))
				return;

			if (MessageBox.Show(
				Resources.CopyOperationMessage,
				ProductInfo.Title,
				MessageBoxButton.OKCancel,
				MessageBoxImage.Information,
				MessageBoxResult.OK) != MessageBoxResult.OK)
				return;

			RecordToDesktop(OperationFileName, content);
		}

		/// <summary>
		/// Records exception log to AppData and Desktop.
		/// </summary>
		/// <param name="exception">Exception</param>
		/// <remarks>
		/// The log file will be appended with new exception as long as one day has not yet passed
		/// since last write. Otherwise, the log file will be overwritten.
		/// </remarks>
		public static void RecordException(Exception exception)
		{
			var content = ComposeHeader() + Environment.NewLine
				+ exception.ToDetailedString() + Environment.NewLine + Environment.NewLine;

			RecordToAppData(ExceptionFileName, content, 10);

			if (MessageBox.Show(
				Resources.RecordExceptionMessage,
				ProductInfo.Title,
				MessageBoxButton.YesNo,
				MessageBoxImage.Error,
				MessageBoxResult.Yes) != MessageBoxResult.Yes)
				return;

			RecordToDesktop(ExceptionFileName, content, 10);
		}

		#region Helper

		private static void RecordToTemp(string fileName, string content, int maxCount = 1)
		{
			try
			{
				var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

				UpdateText(tempFilePath, content, maxCount);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to Temp." + Environment.NewLine
					+ ex);
			}
		}

		private static void RecordToAppData(string fileName, string content, int maxCount = 1)
		{
			try
			{
				FolderService.AssureAppDataFolder();

				var appDataFilePath = Path.Combine(
					FolderService.AppDataFolderPath,
					fileName);

				UpdateText(appDataFilePath, content, maxCount);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to AppData." + Environment.NewLine
					+ ex);
			}
		}

		private static bool TryReadFromAppData(string fileName, out string content)
		{
			var appDataFilePath = Path.Combine(
				FolderService.AppDataFolderPath,
				fileName);

			if (File.Exists(appDataFilePath))
			{
				try
				{
					using (var sr = new StreamReader(appDataFilePath, Encoding.UTF8))
					{
						content = sr.ReadToEnd();
						return true;
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine("Failed to read log from AppData." + Environment.NewLine
						+ ex);
				}
			}
			content = null;
			return false;
		}

		private static void RecordToDesktop(string fileName, string content, int maxCount = 1)
		{
			try
			{
				var desktopFilePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					fileName);

				UpdateText(desktopFilePath, content, maxCount);
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

		private static void UpdateText(string filePath, string newContent, int maxCount)
		{
			string oldContent = null;

			if ((1 < maxCount) && File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
					oldContent = sr.ReadToEnd();

				oldContent = TruncateSections(oldContent, HeaderStart, maxCount - 1);
			}

			SaveText(filePath, oldContent + newContent);
		}

		private static string TruncateSections(string source, string sectionHeader, int sectionCount)
		{
			if (string.IsNullOrEmpty(sectionHeader))
				throw new ArgumentNullException(nameof(sectionHeader));
			if (sectionCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(sectionCount), sectionCount, "The count must be greater than 0.");

			if (string.IsNullOrEmpty(source))
				return string.Empty;

			var firstIndex = source.StartsWith(sectionHeader, StringComparison.Ordinal) ? new[] { 0 } : Enumerable.Empty<int>();
			var secondIndices = source.IndicesOf('\n' /* either CR+Lf or Lf */ + sectionHeader, StringComparison.Ordinal).Select(x => x + 1);
			var indices = firstIndex.Concat(secondIndices).ToArray();

			return source.Substring(indices[Math.Max(0, indices.Length - sectionCount)]);
		}

		#endregion
	}
}