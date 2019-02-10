using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Monitorian.Models
{
	internal class TrapService
	{
		public static void Start()
		{
#if DEBUG
			ConsoleService.TryStartWrite();
#endif

			App.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		public static void End()
		{
			App.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;

			ConsoleService.EndWrite();
		}

		private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			OnException(sender, e.Exception, nameof(Application.DispatcherUnhandledException));
			//e.Handled = true;
		}

		private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			OnException(sender, e.Exception, nameof(TaskScheduler.UnobservedTaskException));
			//e.SetObserved();
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			OnException(sender, (Exception)e.ExceptionObject, nameof(AppDomain.UnhandledException));
		}

		private static void OnException(object sender, Exception exception, string exceptionName)
		{
			if (ConsoleService.Write(exception, exceptionName))
				return;

			LogService.RecordException(exception);
		}
	}
}