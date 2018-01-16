using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models
{
	internal static class FolderService
	{
		public static string AppDataFolderPath
		{
			get
			{
				if (_appDataFolderPath == null)
				{
					var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					if (string.IsNullOrEmpty(appDataPath)) // This should not happen.
						throw new DirectoryNotFoundException();

					_appDataFolderPath = Path.Combine(appDataPath, Assembly.GetExecutingAssembly().GetName().Name);
				}
				return _appDataFolderPath;
			}
		}
		private static string _appDataFolderPath;

		public static void AssureAppDataFolder()
		{
			if (!Directory.Exists(AppDataFolderPath))
				Directory.CreateDirectory(AppDataFolderPath);
		}
	}
}