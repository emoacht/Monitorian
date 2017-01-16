using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models
{
	internal class FolderService
	{
		private static string _folderPath;

		public static string GetAppDataFolderPath(bool createsFolder)
		{
			if (string.IsNullOrWhiteSpace(_folderPath))
			{
				_folderPath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					Assembly.GetExecutingAssembly().GetName().Name);
			}

			if (createsFolder && !Directory.Exists(_folderPath))
				Directory.CreateDirectory(_folderPath);

			return _folderPath;
		}
	}
}