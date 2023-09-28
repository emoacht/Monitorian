using System;

using Monitorian.Core.Common;

namespace Monitorian.Core.ViewModels;

public abstract class ViewModelBase : BindableBase, IDisposable
{
	#region IDisposable

	private bool _isDisposed = false;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			// Free any other managed objects here.
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}