using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Core.Models;
using StartupAgency;

namespace Monitorian.Core
{
	public class AppKeeper
	{
		public IReadOnlyCollection<string> FilteredArguments { get; }
		public StartupAgent StartupAgent { get; }

		public AppKeeper(StartupEventArgs e) : this(e, LanguageService.Options)
		{ }

		public AppKeeper(StartupEventArgs e, params IEnumerable<string>[] ignorableOptions)
		{
			FilteredArguments = Array.AsReadOnly(FilterArguments(e, ignorableOptions.SelectMany(x => x).ToArray()));
			StartupAgent = new StartupAgent();
		}

		private static string[] FilterArguments(StartupEventArgs e, string[] ignorableOptions)
		{
			if (!(e?.Args?.Any() == true))
				return Array.Empty<string>();

			// First element of StartupEventArgs.Args is not executing assembly's path unlike
			// that of arguments provided by Environment.GetCommandLineArgs method.
			return e.Args
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Where(x => !ignorableOptions.Contains(x.ToLower()))
				.ToArray();
		}

		public bool Start()
		{
#if DEBUG
			ConsoleService.TryStartWrite();
#else
			if (FilteredArguments.Any())
				ConsoleService.TryStartWrite();
#endif

			SubscribeExceptions();

			var (success, response) = StartupAgent.Start(ProductInfo.Product, ProductInfo.StartupTaskId, FilteredArguments);
			if (!success && !(response is null))
			{
				ConsoleService.Write(response.ToString());
			}
			return success;
		}

		public void End()
		{
			StartupAgent.Dispose();
			UnsubscribeExceptions();
			ConsoleService.EndWrite();
		}

		public void Write(object content)
		{
			var buffer = content?.ToString();
			if (!string.IsNullOrEmpty(buffer))
				ConsoleService.Write(buffer);
		}

		#region Exception

		private void SubscribeExceptions()
		{
			Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		private void UnsubscribeExceptions()
		{
			Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		}

		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			OnException(sender, e.Exception, nameof(Application.DispatcherUnhandledException));
			//e.Handled = true;
		}

		private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			OnException(sender, e.Exception, nameof(TaskScheduler.UnobservedTaskException));
			//e.SetObserved();
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			OnException(sender, (Exception)e.ExceptionObject, nameof(AppDomain.UnhandledException));
		}

		private void OnException(object sender, Exception exception, string exceptionName)
		{
			if (ConsoleService.Write(exception, exceptionName))
				return;

			LogService.RecordException(exception);
		}

		#endregion
	}
}