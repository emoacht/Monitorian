using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Views;
using StartupAgency;

namespace Monitorian.Core;

public class AppKeeper
{
	public StartupAgent StartupAgent { get; }

	public AppKeeper()
	{
		StartupAgent = new StartupAgent();
	}

	public Task<bool> StartAsync(StartupEventArgs e) => StartAsync(e, null);

	public async Task<bool> StartAsync(StartupEventArgs e, IEnumerable<string> additionalOptions)
	{
		// This method must be called before StandardArguments or OtherArguments property is consumed.
		// An exception thrown in this method will not be handled.
		await ParseArgumentsAsync(e, EnumerateStandardOptions().Concat(additionalOptions ?? []).ToArray());
#if DEBUG
		ConsoleService.TryStartWrite();
#else
		if (OtherArguments.Any())
			ConsoleService.TryStartWrite();
#endif

		SubscribeExceptions();

		var (success, response) = StartupAgent.Start(ProductInfo.Product, ProductInfo.StartupTaskId, ForwardingArguments);
		if (!success && (response is not null))
		{
			ConsoleService.WriteLine(response);
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
			ConsoleService.WriteLine(content);
	}

	#region Arguments

	public static IReadOnlyList<string> StandardArguments => _standardArguments?.ToArray() ?? [];
	private static string[] _standardArguments;

	public static IReadOnlyList<string> OtherArguments => _otherArguments?.ToArray() ?? [];
	private static string[] _otherArguments;

	public static IReadOnlyList<string> ForwardingArguments => _forwardingArguments?.ToArray() ?? [];
	private static string[] _forwardingArguments;

	public static IEnumerable<string> EnumerateStandardOptions() =>
		new[]
		{
			StartupAgent.Options,
			MonitorManager.Options,
			LanguageService.Options,
			WindowPainter.Options
		}
		.SelectMany(x => x);

	private async Task ParseArgumentsAsync(StartupEventArgs e, string[] standardOptions)
	{
		// Divide current arguments.
		// The first element of StartupEventArgs.Args is not executing assembly's path unlike
		// that of arguments provided by Environment.GetCommandLineArgs method.
		var (currentStandard, currentOther) = Divide(e.Args);

		// Divide persistent arguments.
		var persistentArgs = (await LoadArgumentsAsync())?.Split().Select(x => x.Trim('"'));
		var (persistentStandard, persistentOther) = Divide(persistentArgs);

		_standardArguments = persistentStandard.Concat(currentStandard).ToArray();
		_forwardingArguments = currentOther.ToArray();
		_otherArguments = persistentOther.Concat(_forwardingArguments).ToArray();

		(IEnumerable<string> standard, IEnumerable<string> other) Divide(IEnumerable<string> args)
		{
			const char optionMark = '/';
			var isStandard = false;

			var buffer = args?
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.GroupBy(x => (x[0] == optionMark) ? (isStandard = standardOptions.Contains(x.ToLower())) : isStandard)
				.ToArray() ?? [];

			return (standard: buffer.SingleOrDefault(x => x.Key) ?? Enumerable.Empty<string>(),
					other: buffer.SingleOrDefault(x => !x.Key) ?? Enumerable.Empty<string>());
		}
	}

	private const string ArgumentsFileName = "arguments.txt";

	public Task<string> LoadArgumentsAsync() => AppDataService.ReadAsync(ArgumentsFileName);

	public Task SaveArgumentsAsync(string content) => AppDataService.WriteAsync(ArgumentsFileName, append: false, delete: true, content);

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
		if (ConsoleService.WriteLine(exception, exceptionName))
			return;

		Logger.RecordException(exception);
	}

	#endregion
}