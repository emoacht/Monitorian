using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Helper
{
	/// <summary>
	/// A simple JSON-like serialization
	/// </summary>
	public class SimpleSerialization
	{
		public static bool IsPrettified { get; set; } = true;
		public static string Indent { get; set; } = "  "; // This is default value of System.Xml.XmlWriterSettings.IndentChars.
		public static string LineBreak { get; set; } = Environment.NewLine;

		private static (string indent, string lineBreak) GetIndentLineBreak() => IsPrettified ? (Indent, LineBreak) : (string.Empty, " ");

		/// <summary>
		/// Serializes an object whose members are provided by an array of name/value pairs.
		/// </summary>
		/// <param name="members">Array of name/value pairs which provides the members</param>
		/// <returns>Serialized string</returns>
		/// <remarks>
		/// If an Object of value is a String which represents a serialized object, use StringWrapper
		/// class to wrap the String so that it will not be handled as a simple String.
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

	public class StringWrapper
	{
		private readonly string _value;

		public StringWrapper(string value) => this._value = value;

		public override string ToString() => _value;
	}
}