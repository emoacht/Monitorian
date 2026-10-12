using Monitorian.Core;
using Monitorian.Models;

namespace Monitorian;

public class AppController1 : AppController
{
	public new Settings1 Settings => (Settings1)base.Settings;

	public AppController1(AppKeeper keeper) : base(keeper, new Settings1())
	{
	}
}