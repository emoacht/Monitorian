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

		public static string SaveAsHtml(string inputFileName, string inputFileContent)
		{
			var outputFilePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(inputFileName)}.html");

			SaveAsHtml(inputFileName, inputFileContent, outputFilePath);

			return outputFilePath;
		}

		public static void SaveAsHtml(string inputFileName, string inputFileContent, string outputFilePath)
		{
			if (string.IsNullOrWhiteSpace(inputFileName))
				throw new ArgumentNullException(nameof(inputFileName));
			if (string.IsNullOrWhiteSpace(outputFilePath))
				throw new ArgumentNullException(nameof(outputFilePath));

			var title = Path.GetFileNameWithoutExtension(inputFileName);
			var body = inputFileContent.Replace("\r\n", "<br>\r\n");
			var content = BuildHtml(title, body);

			using (var sw = new StreamWriter(outputFilePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(content);
		}

		private static string BuildHtml(string title, string body)
		{
			return $@"<!DOCTYPE HTML PUBLIC "" -//W3C//DTD HTML 4.01//EN"" ""http://www.w3.org/TR/html4/strict.dtd"">
<html>
<head>
<meta charset=""utf-8""/>
<title>{title}</title>
<style type=""text/css"">
<!-- body {{ font-family: monospace; }} -->
</style>
</head>
<body>
{body}
</body>
</html>";
		}
	}
}