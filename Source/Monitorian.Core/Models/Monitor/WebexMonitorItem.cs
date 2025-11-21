using System;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Cisco Webex desk controlled via xAPI
/// </summary>
internal class WebexMonitorItem : MonitorItem
{
	private readonly WebexClient _client;

	public override bool IsBrightnessSupported => true;
	public override bool IsContrastSupported => false;

	public WebexMonitorItem(
		string deviceInstanceId,
		string description,
		string host,
		int port,
		string username,
		string password) : base(
			deviceInstanceId: deviceInstanceId,
			description: description,
			displayIndex: 0,
			monitorIndex: 0,
			monitorRect: Rect.Empty,
			isInternal: false,
			isReachable: true)
	{
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));

		_client = new WebexClient(host, port, username, password);
	}

	public override AccessResult UpdateBrightness(int value = -1)
	{
		try
		{
			var task = _client.GetBacklightAsync();
			task.Wait(); // Synchronous call from async

			var brightness = task.Result;
			if (brightness >= 0)
			{
				this.Brightness = brightness;
				return AccessResult.Succeeded;
			}
			else
			{
				this.Brightness = -1;
				return new AccessResult(AccessStatus.Failed, "Failed to read Webex backlight level");
			}
		}
		catch (Exception ex)
		{
			this.Brightness = -1;
			return new AccessResult(AccessStatus.Failed, $"Webex communication error: {ex.Message}");
		}
	}

	public override AccessResult SetBrightness(int brightness)
	{
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be from 0 to 100.");

		try
		{
			var task = _client.SetBacklightAsync(brightness);
			task.Wait(); // Synchronous call from async

			if (task.Result)
			{
				this.Brightness = brightness;
				return AccessResult.Succeeded;
			}
			else
			{
				return new AccessResult(AccessStatus.Failed, "Failed to set Webex backlight level");
			}
		}
		catch (Exception ex)
		{
			return new AccessResult(AccessStatus.Failed, $"Webex communication error: {ex.Message}");
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
			_client?.Dispose();
		}

		_isDisposed = true;
		base.Dispose(disposing);
	}

	#endregion
}
