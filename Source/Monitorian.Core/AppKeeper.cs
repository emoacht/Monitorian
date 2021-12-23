using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Views;
using StartupAgency;

namespace Monitorian.Core
{
	public class AppKeeper
	{
		public StartupAgent StartupAgent { get; }

		public AppKeeper()
		{
			StartupAgent = new StartupAgent();
		}

		public Task<bool> StartAsync(StartupEventArgs e) => StartAsync(e, GetDefinedOptions());

		public async Task<bool> StartAsync(StartupEventArgs e, params string[] definedOptions)
		{
			// This method must be called before DefinedArguments or OtherArguments property is consumed.
			// An exception thrown in this method will not be handled.
			await ParseArgumentsAsync(e, definedOptions);
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

		#region Arguments

		public static IReadOnlyList<string> DefinedArguments => _definedArguments?.ToArray() ?? Array.Empty<string>();
		private static string[] _definedArguments;

		public static IReadOnlyList<string> OtherArguments => _otherArguments?.ToArray() ?? Array.Empty<string>();
		private static string[] _otherArguments;

		public static string[] GetDefinedOptions() =>
			new[]
			{
				StartupAgent.Options,
				MonitorManager.Options,
				LanguageService.Options,
				WindowPainter.Options
			}
			.SelectMany(x => x)
			.ToArray();

		private async Task ParseArgumentsAsync(StartupEventArgs e, string[] definedOptions)
		{
			// Load persistent arguments.
			var args = (await LoadArgumentsAsync())?.Split() ?? Array.Empty<string>();

			// Concatenate current and persistent arguments.
			// The first element of StartupEventArgs.Args is not executing assembly's path unlike
			// that of arguments provided by Environment.GetCommandLineArgs method.
			args = e.Args.Concat(args).ToArray();
			if (args is not { Length: > 0 })
				return;

			const char optionMark = '/';
			var isDefined = false;

			var buffer = args
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.GroupBy(x => (x[0] == optionMark) ? (isDefined = definedOptions.Contains(x.ToLower())) : isDefined)
				.ToArray();

			_definedArguments = buffer.SingleOrDefault(x => x.Key)?.ToArray();
			_otherArguments = buffer.SingleOrDefault(x => !x.Key)?.ToArray();
		}

		private const string ArgumentsFileName = "arguments.txt";

		public Task<string> LoadArgumentsAsync() => AppDataService.ReadAsync(ArgumentsFileName);

		public Task SaveArgumentsAsync(string content) => AppDataService.WriteAsync(ArgumentsFileName, false, content);

		#endregion

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

			Logger.RecordException(exception);
		}

		#endregion
	}
}