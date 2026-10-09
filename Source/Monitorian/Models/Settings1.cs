using System.Runtime.Serialization;
using System.Threading.Tasks;

using Monitorian.Core.Models;

namespace Monitorian.Models;

[DataContract]
public class Settings1 : Settings
{
	public Settings1() : base()
	{ }

	protected override Task InitiateAsync()
	{
		return base.InitiateAsync();
	}
}