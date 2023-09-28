using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Monitorian.Core.Models;

public static class TempService
{
	/// <summary>
	/// Attempts to get file path in temp folder.
	/// </summary>
	/// <param name="fileName">File name</param>
	/// <param name="fileExtension">
	/// File extension
	/// - Null: File extension part in file name is kept intact.
	/// - Empty: File extension part in file name is removed.
	/// - Other than null or empty: File extension part in file name is replaced.
	/// </param>
	/// <param name="tempFilePath">File path in temp folder</param>
	/// <returns>True if successfully gets</returns>
	public static bool TryGetFilePath(string fileName, string fileExtension, out string tempFilePath)
	{
		if (!string.IsNullOrWhiteSpace(fileName))
		{
			// Path.GetTempPath method does not verify that the path exists or test to see
			// if the current process can access the path.
			// https://learn.microsoft.com/en-us/dotnet/api/system.io.path.gettemppath
			var tempPath = Path.GetTempPath();
			if (Directory.Exists(tempPath))
			{
				string fileSeparator = null;

				if (fileExtension is not null)
				{
					fileName = Path.GetFileNameWithoutExtension(fileName);

					fileExtension = fileExtension.Trim().TrimStart('.').ToLower();
					if (fileExtension.Length > 0)
						fileSeparator = ".";
				}
				tempFilePath = Path.Combine(tempPath, $"monitorian_{fileName.ToLower()}{fileSeparator}{fileExtension}");
				return true;
			}
		}
		tempFilePath = null;
		return false;
	}

	public static (bool success, string filePath) SaveFile(string fileName, string fileExtension, string content)
	{
		if (!TryGetFilePath(fileName, fileExtension, out var tempFilePath))
			return (false, null);

		try
		{
			using var sw = new StreamWriter(tempFilePath, false, Encoding.UTF8); // BOM will be emitted.
			sw.Write(content);

			if (File.Exists(tempFilePath))
				return (true, tempFilePath);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to save a file." + Environment.NewLine
				+ ex);
		}
		return (false, null);
	}

	public static bool DeleteFile(string fileName, string fileExtension, TimeSpan validDuration = default)
	{
		if (!TryGetFilePath(fileName, fileExtension, out var tempFilePath))
			return false;

		var fileInfo = new FileInfo(tempFilePath);
		if (!fileInfo.Exists)
			return false;

		if ((validDuration == default) || (fileInfo.LastWriteTime < DateTime.Now.Add(-validDuration)))
		{
			try
			{
				fileInfo.Delete();
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to delete a file." + Environment.NewLine
					+ ex);
			}
		}
		return false;
	}
}