using Monitorian.Core;
using Monitorian.Models;

namespace Monitorian;

public class AppController : AppControllerCore
{
	public new Settings Settings => (Settings)base.Settings;

	public AppController(AppKeeper keeper) : base(keeper, new Settings())
	{
	}
}