using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Watcher
{
	public class BrightnessConnector : Monitorian.Supplement.BrightnessConnector
	{
		/// <summary>
		/// Options
		/// </summary>
		public static IReadOnlyCollection<string> Options => new[] { ConnectOption };

		private const string ConnectOption = "/connect";

		public override bool CanConnect => _canConnect && base.CanConnect;
		private readonly bool _canConnect = false;

		public BrightnessConnector() : base()
		{
			if (AppKeeper.StandardArguments.Select(x => x.ToLower()).Contains(ConnectOption))
			{
				_canConnect = true;
			}
		}

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
			}

			// Free any unmanaged objects here.
			_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion
	}
}