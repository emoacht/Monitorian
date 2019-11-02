using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models
{
	internal class DocumentService
	{
		public static string ReadEmbeddedFile(string fileName)
		{
			var assembly = Assembly.GetEntryAssembly();

			try
			{
				var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(fileName));
				if (resourceName is null)
					return null;

				using (var s = assembly.GetManifestResourceStream(resourceName))
				using (var sr = new StreamReader(s))
					return sr.ReadToEnd();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to read an embedded file." + Environment.NewLine
					+ ex);
				return null;
			}
		}

		public static string SaveTempFileAsHtml(string fileName, string title, string body)
		{
			var filePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}.html");
			SaveFileAsHtml(filePath, title, body);
			return filePath;
		}

		public static void SaveFileAsHtml(string filePath, string title, string body)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			if (string.IsNullOrWhiteSpace(title))
				title = Path.GetFileNameWithoutExtension(filePath);

			var html = BuildHtml(title, body);

			using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(html);
		}

		private static string BuildHtml(string title, string body)
		{
			body = body?
				.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x =>
				{
					var array = x.Split(Array.Empty<char>(), 2, StringSplitOptions.RemoveEmptyEntries);
					var (tag, content) = array.First() switch
					{
						"#" => ("h1", array.Last()),
						"##" => ("h2", array.Last()),
						"###" => ("h3", array.Last()),
						_ => ("p", x)
					};
					return $"<{tag}>{content}</{tag}>";
				})
				.Aggregate((w, n) => $"{w}\r\n{n}");

			return $@"<!DOCTYPE HTML PUBLIC "" -//W3C//DTD HTML 4.01//EN"" ""http://www.w3.org/TR/html4/strict.dtd"">
<html>
<head>
<meta charset=""utf-8""/>
<title>{title}</title>
<style type=""text/css"">
<!-- body {{ margin: 0 30px; font-family: Consolas, monospace; }} -->
</style>
</head>
<body>
{body}
</body>
</html>";
		}
	}
}