using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	internal interface IDisplayItem
	{
		public string DeviceInstanceId { get; }
		public string DisplayName { get; }
		public bool IsInternal { get; }
		public string ConnectionDescription { get; }
	}

	internal class DisplayMonitor
	{
		#region Type

		[DataContract]
		public class DisplayItem : IDisplayItem
		{
			[DataMember(Order = 0)]
			public string DeviceInstanceId { get; }

			[DataMember(Order = 1)]
			public string DisplayName { get; }

			[DataMember(Order = 2)]
			public float PhysicalDiagonalLength { get; }

			[DataMember(Order = 3)]
			public bool IsInternal { get; }

			[DataMember(Order = 4)]
			public string ConnectionDescription { get; }

			public DisplayItem(Monitorian.Supplement.DisplayInformation.DisplayItem item)
			{
				this.DeviceInstanceId = item.DeviceInstanceId;
				this.DisplayName = item.DisplayName;
				this.PhysicalDiagonalLength = item.PhysicalDiagonalLength;
				this.IsInternal = item.IsInternal;
				this.ConnectionDescription = item.ConnectionDescription;
			}
		}

		#endregion

		public static Task<DisplayItem[]> GetDisplayMonitorsAsync()
		{
			return Monitorian.Supplement.DisplayInformation.GetDisplayMonitorsAsync()
				.ContinueWith(task => task.Result.Select(x => new DisplayItem(x)).ToArray());
		}
	}
}