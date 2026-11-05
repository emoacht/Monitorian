using System;
using System.Collections;
using System.Linq;

namespace Monitorian.Core.Helper;

/// <summary>
/// Simple JSON-like serialization
/// </summary>
public class SimpleSerialization
{
	public static bool IsPrettified { get; set; } = true;
	public static string Indent { get; set; } = "  "; // This is default value of System.Xml.XmlWriterSettings.IndentChars.
	public static string LineBreak { get; set; } = Environment.NewLine;

	private static (string indent, string lineBreak) GetIndentLineBreak() => IsPrettified ? (Indent, LineBreak) : (string.Empty, " ");

	/// <summary>
	/// Serializes object whose members are provided by array of name/value pairs.
	/// </summary>
	/// <param name="members">Array of name/value pairs</param>
	/// <returns>Serialized string</returns>
	/// <remarks>
	/// If value is string representing non-string object (e.g. pre-serialized object, pre-formatted
	/// number), use <see cref="UnquotedValue"/> to wrap the value so that it is emitted as-is
	/// without quoting unlike ordinary string.
	/// </remarks>
	public static string Serialize(params (string name, object value)[] members)
	{
		var (indent, lineBreak) = GetIndentLineBreak();

		var memberStrings = members.Select(x => $@"{indent}""{Escape(x.name)}"": {Convert(x.value)}");

		return $@"{{{lineBreak}{string.Join($",{lineBreak}", memberStrings)}{lineBreak}}}";
	}

	private static string Escape(string source) => source.Replace("\r", @"\r").Replace("\n", @"\n");

	private static string Convert(object value)
	{
		return value switch
		{
			null => "null",
			bool v => v ? "true" : "false",
			string v => $@"""{Escape(v)}""",
			Enum v => $@"""{v}""",
			DateTime v => $@"""{v:yyyy-MM-ddTHH:mm:ss.fffZ}""",
			DateTimeOffset v => $@"""{v:yyyy-MM-ddTHH:mm:ss.fffzzz}""",
			IEnumerable v => ConvertCollection(v),
			_ => ConvertValue(value.ToString())
		};
	}

	private static string ConvertCollection(IEnumerable values)
	{
		var (indent, lineBreak) = GetIndentLineBreak();

		var elementStrings = values.Cast<object>().Select(x => ConvertCollectionValue(Convert(x)));

		return $"[{lineBreak}{indent}{string.Join($",{lineBreak}{indent}", elementStrings)}{lineBreak}{indent}]";
	}

	public static string ConvertValue(string value) =>
		IsPrettified ? value.Replace(LineBreak, LineBreak + Indent) : value;

	public static string ConvertCollectionValue(string value) =>
		IsPrettified ? Indent + ConvertValue(value) : value;
}

/// <summary>
/// Container of string which is to be emitted without quoting
/// </summary>
/// <param name="value">String representing non-string object</param>
/// <remarks>
/// This container is intended to wrap string representing non-string object and emit as-is without
/// quoting unlike ordinary string.
/// </remarks>
public class UnquotedValue(string value)
{
	private readonly string _value = value;

	public override string ToString() => _value;
}