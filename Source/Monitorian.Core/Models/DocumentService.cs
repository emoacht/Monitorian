using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Monitorian.Core.Models;

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

			using var s = assembly.GetManifestResourceStream(resourceName);
			using var sr = new StreamReader(s);
			return sr.ReadToEnd();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to read an embedded file." + Environment.NewLine
				+ ex);
			return null;
		}
	}

	public static string BuildHtml(string fileName, string title, string body)
	{
		if (string.IsNullOrWhiteSpace(title) &&
			!string.IsNullOrWhiteSpace(fileName))
			title = Path.GetFileNameWithoutExtension(fileName);

		body = body?
			.Split(["\r\n\r\n", "\n\n" /* two consecutive line breaks */ ], StringSplitOptions.RemoveEmptyEntries)
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

		return $@"<!DOCTYPE html>
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