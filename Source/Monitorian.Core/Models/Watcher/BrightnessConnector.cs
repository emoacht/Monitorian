using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Watcher
{
	public class BrightnessConnector : Monitorian.Supplement.BrightnessConnector
	{
		/// <summary>
		/// Options
		/// </summary>
		public static IReadOnlyCollection<string> Options => new[] { ConnectOption };

		private const string ConnectOption = "/connect";

		public override bool CanConnect => _isSpecified && base.CanConnect;
		private readonly bool _isSpecified;

		public BrightnessConnector() : base()
		{
			_isSpecified = OsVersion.Is10OrGreater &&
				AppKeeper.StandardArguments.Select(x => x.ToLower()).Contains(ConnectOption);
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