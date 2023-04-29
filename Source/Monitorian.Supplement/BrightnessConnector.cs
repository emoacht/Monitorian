using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;

namespace Monitorian.Supplement
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.Graphics.Display.BrightnessOverride"/>
	/// </summary>
	public class BrightnessConnector : IDisposable
	{
		#region Type

		private enum Request
		{
			None = 0,

			/// <summary>
			/// Report illuminance and brightness
			/// </summary>
			Report
		}

		private enum Result
		{
			None = 0,

			/// <summary>
			/// Rrequest is OK.
			/// </summary>
			OK,

			/// <summary>
			/// Request is valid but ambient light sensor (ALS) is unavailable.
			/// </summary>
			Unavailable,

			/// <summary>
			/// Request is invalid.
			/// </summary>
			Invalid
		}

		private class ReportItem
		{
			public DateTimeOffset Date { get; }
			public double Illuminance { get; }
			public int Brightness { get; }

			public ReportItem(DateTimeOffset date, double illuminance, int brightness)
			{
				Date = date;
				Illuminance = illuminance;
				Brightness = brightness;
			}
		}

		private static bool TryFindReport(ValueSet message, out DateTimeOffset date, out double illuminance, out int brightness)
		{
			if (message.TryGetValue(nameof(ReportItem.Date), out object value) && (value is long ticks) &&
				message.TryGetValue(nameof(ReportItem.Illuminance), out value) && (value is double illuminanceBuffer) &&
				message.TryGetValue(nameof(ReportItem.Brightness), out value) && (value is int brightnessBuffer))
			{
				date = new DateTimeOffset(ticks, TimeSpan.Zero);
				illuminance = illuminanceBuffer;
				brightness = brightnessBuffer;
				return true;
			}
			date = default;
			illuminance = default;
			brightness = default;
			return false;
		}

		/// <summary>
		/// Interval in seconds
		/// </summary>
		public float Interval { get; set; } = 0.1F;

		#endregion

		private const string ServiceName = "IlluminoService";
		private const string IdentityName = "IlluminoChecker";

		private readonly Lazy<string> _familyName = new(() => FindFamilyName(IdentityName));

		private static string FindFamilyName(string identityName)
		{
			var package = new PackageManager()
				.FindPackagesForUser(string.Empty)
				.Reverse()
				.FirstOrDefault(x => x.Id.Name?.Contains(identityName) is true);

			return package?.Id.FamilyName;
		}

		/// <summary>
		/// Determines whether a connection with AppService provider can be performed.
		/// </summary>
		public virtual bool CanConnect => !string.IsNullOrEmpty(_familyName.Value) && _isAvailable;
		private bool _isAvailable = true; // default

		/// <summary>
		/// Constructor
		/// </summary>
		public BrightnessConnector()
		{ }

		private Action<int> _onBrightnessChanged;
		private Action<string> _onError;
		private Func<bool> _onContinue;

		/// <summary>
		/// Asynchronously initiates and performs the first connection with AppService provider.
		/// </summary>
		/// <param name="onBrightnessChanged">Action to be invoked when brightness changed</param>
		/// <param name="onError">Action to be invoked when error occurred</param>
		/// <param name="onContinue">Action to be invoked when continuation of AppService is determined</param>
		public virtual async Task InitiateAsync(Action<int> onBrightnessChanged, Action<string> onError, Func<bool> onContinue)
		{
			this._onBrightnessChanged = onBrightnessChanged ?? throw new ArgumentNullException(nameof(onBrightnessChanged));
			this._onError = onError ?? throw new ArgumentNullException(nameof(onError));
			this._onContinue = onContinue ?? throw new ArgumentNullException(nameof(onContinue));

			await ConnectAsync(true);
		}

		private AppServiceConnection _appServiceConnection;

		/// <summary>
		/// Asynchronously opens a connection with AppService provider.
		/// </summary>
		/// <returns>True if successfully opens</returns>
		public virtual async Task<bool> OpenAsync()
		{
			if (_appServiceConnection is not null)
				return true;

			if (!CanConnect)
				return false;

			var appServiceConnection = new AppServiceConnection
			{
				AppServiceName = ServiceName,
				PackageFamilyName = _familyName.Value
			};

			var status = await appServiceConnection.OpenAsync();
			switch (status)
			{
				case AppServiceConnectionStatus.Success:
					_appServiceConnection = appServiceConnection;
					_appServiceConnection.RequestReceived += OnAppServiceConnectionRequestReceived;
					_appServiceConnection.ServiceClosed += OnAppServiceConnectionServiceClosed;
					return true;

				default:
					// https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appservice.appserviceconnectionstatus
					if (status is not AppServiceConnectionStatus.AppUnavailable)
						_isAvailable = false;

					_onError?.Invoke($"Failed: {status}");
					return false;
			}
		}

		private bool _isMultiple;

		/// <summary>
		/// Asynchronously performs a connection with AppService provider.
		/// </summary>
		/// <param name="isMultiple">Whether to request to report multiple times</param>
		/// <returns>True if successfully performs</returns>
		public virtual async Task<bool> ConnectAsync(bool isMultiple)
		{
			_isMultiple = isMultiple;

			if (!(await OpenAsync()))
				return false;

			var requestMessage = new ValueSet
			{
				[nameof(Request)] = nameof(Request.Report)
			};
			if (isMultiple)
			{
				requestMessage.Add(nameof(Interval), Interval);
				requestMessage.Add(nameof(ReportItem), nameof(ReportItem.Brightness));
			}

			var response = await _appServiceConnection.SendMessageAsync(requestMessage);

			var result = response.Message.TryGetValue(nameof(Result), out object value)
				&& Enum.TryParse(value as string, out Result resultBuffer)
					? resultBuffer
					: Result.None;

			switch (response.Status, result)
			{
				case (AppServiceResponseStatus.Success, Result.OK):
					if (TryFindReport(response.Message, out _, out _, out int brightness))
					{
						_onBrightnessChanged?.Invoke(brightness);
					}
					Debug.WriteLine($"Succeeded: {response.Status} | {result}");
					return true;

				default:
					Debug.WriteLine($"Failed: {response.Status} | {result}");
					_onError?.Invoke($"Failed: {response.Status} | {result}");
					return false;
			}
		}

		private void OnAppServiceConnectionRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
		{
			if (TryFindReport(args.Request.Message, out _, out _, out int brightness))
				_onBrightnessChanged?.Invoke(brightness);
		}

		private async void OnAppServiceConnectionServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
		{
			// AppService closes when around 25 seconds has elapsed after it opened.
			Debug.WriteLine("ServiceClosed");
			ReleaseAppServiceConnection();

			if (_isMultiple)
			{
				await Task.Delay(TimeSpan.FromSeconds(Interval));

				if (_onContinue.Invoke())
					await ConnectAsync(true);
			}
		}

		private void ReleaseAppServiceConnection()
		{
			try
			{
				_appServiceConnection.RequestReceived -= OnAppServiceConnectionRequestReceived;
				_appServiceConnection.ServiceClosed -= OnAppServiceConnectionServiceClosed;
				_appServiceConnection?.Dispose();
				_appServiceConnection = null;
			}
			catch (Exception ex)
			{
				_onError?.Invoke($"Failed: {ex.Message}");
			}
		}

		#region IDisposable

		private bool _isDisposed = false;

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				ReleaseAppServiceConnection();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}