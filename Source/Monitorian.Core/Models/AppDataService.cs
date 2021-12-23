using System;
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

		#region Access

		public static IEnumerable<string> EnumerateFileNames(string searchPattern)
		{
			if (!Directory.Exists(FolderPath))
				return Enumerable.Empty<string>();

			return Directory.EnumerateFiles(FolderPath, searchPattern)
				.Select(x => Path.GetFileName(x));
		}

		public static async Task<string> ReadAsync(string fileName)
		{
			var filePath = Path.Combine(FolderPath, fileName);

			if (!File.Exists(filePath))
				return null;

			using var sr = new StreamReader(filePath, Encoding.UTF8);
			return await sr.ReadToEndAsync();
		}

		public static async Task WriteAsync(string fileName, bool append, string content)
		{
			AssureFolder();

			var filePath = Path.Combine(FolderPath, fileName);

			using var sw = new StreamWriter(filePath, append, Encoding.UTF8); // BOM will be emitted.
			await sw.WriteAsync(content);
		}

		public static void Delete(string fileName)
		{
			var filePath = Path.Combine(FolderPath, fileName);
			File.Delete(filePath);
		}

		public static void Rename(string oldFileName, string newFileName)
		{
			var oldFilePath = Path.Combine(FolderPath, oldFileName);
			var newFilePath = Path.Combine(FolderPath, newFileName);
			File.Move(oldFilePath, newFilePath);
		}

		#endregion

		#region Load/Save

		/// <summary>
		/// Loads saved instance from a specified file and copies its properties to current instance.
		/// </summary>
		/// <typeparam name="T">Type of instance</typeparam>
		/// <param name="instance">Current instance</param>
		/// <param name="fileName">File name of saved instance</param>
		/// <param name="knownTypes">Known types of members of instance</param>
		/// <remarks>
		/// Only values of public and instance properties will be copied.
		/// An indexer will be ignored.
		/// </remarks>
		public static void Load<T>(T instance, string fileName, IEnumerable<Type> knownTypes = null) where T : class
		{
			Load(instance, fileName, BindingFlags.Public | BindingFlags.Instance, knownTypes);
		}

		/// <summary>
		/// Loads saved instance from a specified file and copies its properties to current instance.
		/// </summary>
		/// <typeparam name="T">Type of instance</typeparam>
		/// <param name="instance">Current instance</param>
		/// <param name="fileName">File name of saved instance</param>
		/// <param name="flags">Flags to search properties to be copied</param>
		/// <param name="knownTypes">Known types of members of instance</param>
		/// <remarks>
		/// An indexer will be ignored.
		/// </remarks>
		public static void Load<T>(T instance, string fileName, BindingFlags flags, IEnumerable<Type> knownTypes = null) where T : class
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
				var serializer = new DataContractSerializer(type, knownTypes);
				var loaded = (T)serializer.ReadObject(xr);

				type.GetProperties(flags)
					.Where(x => x.CanWrite)
					.Where(x => x.GetIndexParameters().Length == 0) // Exclude indexer to prevent TargetParameterCountException.
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

		/// <summary>
		/// Saves current instance to a specified file.
		/// </summary>
		/// <typeparam name="T">Type of instance</typeparam>
		/// <param name="instance">Current instance</param>
		/// <param name="fileName">File name of saved instance</param>
		/// <param name="knownTypes">Known types of members of instance</param>
		public static void Save<T>(T instance, string fileName, IEnumerable<Type> knownTypes = null) where T : class
		{
			AssureFolder();

			var filePath = Path.Combine(FolderPath, fileName);

			using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
			using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true });

			var type = instance.GetType(); // GetType method works in derived class.
			var serializer = new DataContractSerializer(type, knownTypes);
			serializer.WriteObject(xw, instance);
			xw.Flush();
		}

		#endregion
	}
}