using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models
{
	internal static class FolderService
	{
		public static string AppDataFolderPath => _appDataFolderPath ??= GetAppDataFolderPath();
		private static string _appDataFolderPath;

		private static string GetAppDataFolderPath()
		{
			var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (string.IsNullOrEmpty(appDataPath)) // This should not happen.
				throw new DirectoryNotFoundException();

			return Path.Combine(appDataPath, ProductInfo.Product);
		}

		public static void AssureAppDataFolder()
		{
			if (!Directory.Exists(AppDataFolderPath))
				Directory.CreateDirectory(AppDataFolderPath);
		}
	}
}