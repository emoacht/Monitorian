using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Core.Models;
using Monitorian.Core.Views;
using StartupAgency;

namespace Monitorian.Core
{
	public class AppKeeper
	{
		public static IReadOnlyList<string> DefinedArguments => _definedArguments?.ToArray() ?? Array.Empty<string>();
		private static string[] _definedArguments;

		public static IReadOnlyList<string> OtherArguments => _otherArguments?.ToArray() ?? Array.Empty<string>();
		private static string[] _otherArguments;

		public StartupAgent StartupAgent { get; }

		public static string[] GetDefinedOptions() =>
			new[]
			{
				StartupAgent.Options,
				LanguageService.Options,
				SettingsCore.Options,
				WindowEffect.Options
			}
			.SelectMany(x => x)
			.ToArray();

		public AppKeeper(StartupEventArgs e) : this(e?.Args, GetDefinedOptions())
		{ }

		public AppKeeper(string[] args, params string[] definedOptions)
		{
			if (args?.Any() is true)
			{
				const char optionMark = '/';
				var isDefined = false;

				// First element of StartupEventArgs.Args is not executing assembly's path unlike
				// that of arguments provided by Environment.GetCommandLineArgs method.
				var buffer = args
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.GroupBy(x => (x[0] == optionMark) ? (isDefined = definedOptions.Contains(x.ToLower())) : isDefined)
					.ToArray();

				_definedArguments = buffer.SingleOrDefault(x => x.Key)?.ToArray();
				_otherArguments = buffer.SingleOrDefault(x => !x.Key)?.ToArray();
			}

			StartupAgent = new StartupAgent();
		}

		public bool Start()
		{
#if DEBUG
			ConsoleService.TryStartWrite();
#else
			if (OtherArguments.Any())
				ConsoleService.TryStartWrite();
#endif

			SubscribeExceptions();

			var (success, response) = StartupAgent.Start(ProductInfo.Product, ProductInfo.StartupTaskId, OtherArguments);
			if (!success && (response is not null))
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

		public void Write(string content)
		{
			if (!string.IsNullOrEmpty(content))
				ConsoleService.Write(content);
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