using Monitorian.Core;
using Monitorian.Models;

namespace Monitorian;

public class AppController1 : AppController
{
	public new Settings Settings => (Settings)base.Settings;

	public AppController1(AppKeeper keeper) : base(keeper, new Settings())
	{
	}
}