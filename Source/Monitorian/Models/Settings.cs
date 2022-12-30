using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Models;

namespace Monitorian.Models
{
	[DataContract]
	public class Settings : SettingsCore
	{
		public Settings() : base()
		{ }

		protected override Task InitiateAsync()
		{
			return base.InitiateAsync();
		}
	}
}