using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models
{
	public class ProductInfo
	{
		private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

		public static Version Version { get; } = _assembly.GetName().Version;

		public static string Title { get; } = GetAttribute<AssemblyTitleAttribute>(_assembly).Title;

		private static TAttribute GetAttribute<TAttribute>(Assembly assembly) where TAttribute : Attribute =>
			(TAttribute)Attribute.GetCustomAttribute(assembly, typeof(TAttribute));
	}
}