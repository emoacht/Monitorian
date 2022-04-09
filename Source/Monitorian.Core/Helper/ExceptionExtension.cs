using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Helper
{
	public static class ExceptionExtension
	{
		public static string ToDetailedString(this Exception ex)
		{
			var buffer = new StringBuilder(ex.GetType().ToString());
			buffer.Append($": {ex.Message} HResult: {ex.HResult}");

			IEnumerable<Exception> innerExceptions;
			if (ex is AggregateException ae)
			{
				innerExceptions = ae.Flatten().InnerExceptions.AsEnumerable();
			}
			else
			{
				innerExceptions = (ex.InnerException is not null)
					? new Exception[] { ex.InnerException }
					: Array.Empty<Exception>();

				foreach (var property in EnumerateAddedProperties(ex))
				{
					buffer.Append($" {property.Name}: {property.GetValue(ex) ?? "NULL"}");
				}
			}

			foreach (var ie in innerExceptions)
			{
				buffer.AppendLine();
				buffer.AppendLine($" ---> {ie.ToDetailedString()}");
				buffer.Append($"   {EndOfInnerExceptionStack.Value}");
			}

			var stackTrace = ex.StackTrace;
			if (!string.IsNullOrEmpty(stackTrace))
			{
				buffer.AppendLine();
				buffer.Append(stackTrace);
			}

			return buffer.ToString();
		}

		private static IEnumerable<PropertyInfo> EnumerateAddedProperties(Exception ex)
		{
			return ex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(x => !BasePropertyNames.Value.Contains(x.Name));
		}

		private static readonly Lazy<string[]> BasePropertyNames = new(() =>
			typeof(Exception).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToArray());

		private static readonly Lazy<string> EndOfInnerExceptionStack = new(() =>
			GetEnvironmentString("Exception_EndOfInnerExceptionStack"));

		private static string GetEnvironmentString(string key)
		{
			var method = typeof(Environment).GetMethod("GetResourceString", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string) }, null);

			string buffer = null;
			try
			{
				buffer = method?.Invoke(null, new object[] { key }) as string;
			}
			catch
			{
			}
			return buffer ?? key; // Fallback
		}
	}
}