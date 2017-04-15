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

		/// <summary>
		/// Gets folder path to this application's folder in local AppData.
		/// </summary>
		/// <param name="createsFolder">Whether to create this application's folder if it does not exist</param>
		/// <returns>This method should not throw an exception because the folder is in local AppData.</returns>
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