using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using Monitorian.Core.Helper;
using Monitorian.Core.Properties;

namespace Monitorian.Core.Models
{
	public class Logger
	{
		private const string HeaderStart = "[Date:";
		private static string ComposeHeader() => $"{HeaderStart} {DateTime.Now} Ver: {ProductInfo.Version}]";

		#region Probe

		private const string ProbeFileName = "probe.log";

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

		#endregion

		#region Operation

		private const string OperationFileName = "operation.log";
		private const string DateFormat = "yyyyMMdd";
		private static string GetOperationDateFileName(DateTimeOffset date) => $"operation{date.ToString(DateFormat)}.log";

		/// <summary>
		/// The number of days before expiration of operation log file
		/// </summary>
		public static byte ExpiryDays { get; set; } = 6;

		public static Task PrepareOperationAsync()
		{
			try
			{
				return GetOperationFileNamesAsync();
			}
			catch (Exception ex)
			{
				return Task.FromException(ex);
			}
		}

		private static async Task<string[]> GetOperationFileNamesAsync()
		{
			var expiryDate = DateTimeOffset.Now.Date.AddDays(-ExpiryDays);
			var fileNamePattern = new Regex(@"^operation(?<date>[0-9]{8}).log$");
			var fileNames = new List<string>();

			foreach (var fileName in AppDataService.EnumerateFileNames("*.log").OrderBy(x => x))
			{
				var match = fileNamePattern.Match(fileName);
				if (match.Success)
				{
					if (DateTimeOffset.TryParseExact(match.Groups["date"].Value,
						DateFormat,
						null,
						DateTimeStyles.None,
						out DateTimeOffset fileDate)
						&& (expiryDate <= fileDate))
					{
						fileNames.Add(fileName);
						continue;
					}

					AppDataService.Delete(fileName);
				}
			}

			#region Conversion

			var content = await AppDataService.ReadAsync(OperationFileName);
			if (content is not null)
			{
				if (fileNames.Any())
				{
					content += await AppDataService.ReadAsync(fileNames.First());
					await AppDataService.WriteAsync(fileNames.First(), append: false, content);
					AppDataService.Delete(OperationFileName);
				}
				else
				{
					var fileName = GetOperationDateFileName(DateTimeOffset.Now);
					AppDataService.Rename(OperationFileName, fileName);
					fileNames.Add(fileName);
				}
			}

			#endregion

			return fileNames.ToArray();
		}

		/// <summary>
		/// Records operation log to AppData.
		/// </summary>
		/// <param name="content">Content</param>
		public static async Task RecordOperationAsync(string content)
		{
			content = ComposeHeader() + Environment.NewLine
				+ content + Environment.NewLine + Environment.NewLine;

			var fileName = GetOperationDateFileName(DateTimeOffset.Now);

			try
			{
				await AppDataService.WriteAsync(fileName, append: true, content);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to AppData." + Environment.NewLine
					+ ex);
			}
		}

		/// <summary>
		/// Copies operation log from AppData to Desktop.
		/// </summary>
		/// <param name="threshold">Threshold of log's content (in the number of characters)</param>
		public static async Task CopyOperationAsync(int threshold = 10000)
		{
			var buffer = new StringBuilder();

			try
			{
				foreach (var fileName in await GetOperationFileNamesAsync())
					buffer.Append(await AppDataService.ReadAsync(fileName));
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to read log from AppData." + Environment.NewLine
					+ ex);
			}

			if (buffer.Length == 0)
				return;

			if (buffer.Length < threshold)
			{
				MessageBox.Show(
					Resources.CopyWaitOperationMessage,
					ProductInfo.Title,
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation,
					MessageBoxResult.OK);

				return;
			}

			if (MessageBox.Show(
				Resources.CopySaveOperationMessage,
				ProductInfo.Title,
				MessageBoxButton.OKCancel,
				MessageBoxImage.Information,
				MessageBoxResult.OK) != MessageBoxResult.OK)
				return;

			RecordToDesktop(OperationFileName, buffer.ToString());
		}

		#endregion

		#region Exception

		private const string ExceptionFileName = "exception.log";

		/// <summary>
		/// Records exception log to AppData and Desktop.
		/// </summary>
		/// <param name="exception">Exception</param>
		/// <param name="capacity">The number of excceptions that the log file can contain</param>
		/// <remarks>
		/// The log file will be appended with new exception as long as one day has not yet passed
		/// since last write. Otherwise, the log file will be overwritten.
		/// </remarks>
		public static void RecordException(Exception exception, int capacity = 8)
		{
			if (exception is null)
				throw new ArgumentNullException(nameof(exception));
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "The capacity must be positive.");

			var content = ComposeHeader() + Environment.NewLine
				+ exception.ToDetailedString() + Environment.NewLine + Environment.NewLine;

			RecordToAppData(ExceptionFileName, content, capacity);

			if (MessageBox.Show(
				Resources.RecordExceptionMessage,
				ProductInfo.Title,
				MessageBoxButton.YesNo,
				MessageBoxImage.Error,
				MessageBoxResult.Yes) != MessageBoxResult.Yes)
				return;

			RecordToDesktop(ExceptionFileName, content, capacity);
		}

		#endregion

		#region Helper

		private static void RecordToTemp(string fileName, string content, int capacity = 1)
		{
			try
			{
				var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

				UpdateContent(tempFilePath, content, capacity);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to Temp." + Environment.NewLine
					+ ex);
			}
		}

		private static void RecordToAppData(string fileName, string content, int capacity = 1)
		{
			try
			{
				var appDataFilePath = Path.Combine(AppDataService.EnsureFolderPath(), fileName);

				UpdateContent(appDataFilePath, content, capacity);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to AppData." + Environment.NewLine
					+ ex);
			}
		}

		private static void RecordToDesktop(string fileName, string content, int capacity = 1)
		{
			try
			{
				var desktopFilePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					fileName);

				UpdateContent(desktopFilePath, content, capacity);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to Desktop." + Environment.NewLine
					+ ex);
			}
		}

		/// <summary>
		/// Updates content of a log file.
		/// </summary>
		/// <param name="filePath">File path</param>
		/// <param name="newContent">New content</param>
		/// <param name="capacity">The number of entries that the log file can contain</param>
		/// <remarks>
		/// The log file will be appended with new content as long as one day has not yet passed
		/// since last write. Otherwise, the log file will be overwritten.
		/// </remarks>
		private static void UpdateContent(string filePath, string newContent, int capacity)
		{
			string oldContent = null;

			if ((1 < capacity) && File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using var sr = new StreamReader(filePath, Encoding.UTF8);
				oldContent = sr.ReadToEnd();

				if (!string.IsNullOrEmpty(oldContent))
					oldContent = TruncateSections(oldContent, HeaderStart, capacity - 1);
			}

			using var sw = new StreamWriter(filePath, false, Encoding.UTF8); // BOM will be emitted.
			sw.Write(oldContent + newContent);

			static string TruncateSections(string content, string sectionHeader, int sectionCount)
			{
				var firstIndex = content.StartsWith(sectionHeader, StringComparison.Ordinal) ? new[] { 0 } : Enumerable.Empty<int>();
				var secondIndices = content.IndicesOf('\n' /* either CR+Lf or Lf */ + sectionHeader, StringComparison.Ordinal).Select(x => x + 1);
				var indices = firstIndex.Concat(secondIndices).ToArray();

				return content.Substring(indices[Math.Max(0, indices.Length - sectionCount)]);
			}
		}

		#endregion
	}
}