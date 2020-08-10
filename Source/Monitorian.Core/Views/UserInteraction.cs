using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Views
{
	public static class UserInteraction
	{
		private const string DeferOption = "/defer";

		public static IReadOnlyCollection<string> Options => new[] { DeferOption };

		internal static bool IsValueDeferred => _isValueDeferred ??= AppKeeper.DefinedArguments.Contains(DeferOption);
		private static bool? _isValueDeferred;
	}
}