using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StartupBridge.Storage;

namespace StartupBridge
{
	/// <summary>
	/// Startup data
	/// </summary>
	public class StartupData
	{
		/// <summary>
		/// Last start time of application
		/// </summary>
		/// <remarks>
		/// The underlying value of this property will be updated when accessed the first time after
		/// system boot.
		/// </remarks>
		public static DateTimeOffset LastStartTime
		{
			get
			{
				if (!_lastStartTime.HasValue)
				{
					_lastStartTime = SettingsAccessor.Local.GetValue<DateTimeOffset>();
					SettingsAccessor.Local.SetValue(DateTimeOffset.Now);
				}
				return _lastStartTime.Value;
			}
		}
		private static DateTimeOffset? _lastStartTime;
	}
}