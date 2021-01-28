﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Monitorian.Core.Models
{
	public static class AppDataService
	{
		public static string FolderPath => _folderPath ??= GetFolderPath();
		private static string _folderPath;

		private static string GetFolderPath()
		{
			var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (string.IsNullOrEmpty(appDataPath)) // This should not happen.
				throw new DirectoryNotFoundException();

			return Path.Combine(appDataPath, ProductInfo.Product);
		}

		public static void AssureFolder()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
		}

		public static void Load<T>(T instance, string fileName) where T : class
		{
			var filePath = Path.Combine(FolderPath, fileName);
			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists || (fileInfo.Length == 0))
				return;

			try
			{
				using var sr = new StreamReader(filePath, Encoding.UTF8);
				using var xr = XmlReader.Create(sr);

				var type = instance.GetType(); // GetType method works in derived class.
				var serializer = new DataContractSerializer(type);
				var loaded = (T)serializer.ReadObject(xr);

				type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.CanWrite)
					.ToList()
					.ForEach(x => x.SetValue(instance, x.GetValue(loaded)));
			}
			catch (SerializationException)
			{
				// Ignore faulty file.
			}
			catch (XmlException)
			{
				// Ignore faulty file.
			}
		}

		public static void Save<T>(T instance, string fileName) where T : class
		{
			AssureFolder();

			var filePath = Path.Combine(FolderPath, fileName);

			using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
			using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true });

			var type = instance.GetType(); // GetType method works in derived class.
			var serializer = new DataContractSerializer(type);
			serializer.WriteObject(xw, instance);
			xw.Flush();
		}
	}
}