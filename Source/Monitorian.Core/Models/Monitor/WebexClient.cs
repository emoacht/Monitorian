using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// HTTP client for Cisco Webex xAPI communication
/// </summary>
internal class WebexClient : IDisposable
{
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;
	private bool _isDisposed;

	public WebexClient(string host, int port, string username, string password)
	{
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));
		if (string.IsNullOrWhiteSpace(username))
			throw new ArgumentNullException(nameof(username));
		if (string.IsNullOrWhiteSpace(password))
			throw new ArgumentNullException(nameof(password));

		_baseUrl = $"https://{host}:{port}";

		// Create HTTP client with Basic authentication
		_httpClient = new HttpClient(new HttpClientHandler
		{
			// Allow self-signed certificates (common for Webex devices)
			ServerCertificateCustomValidationCallback = (_, _, _, _) => true
		})
		{
			BaseAddress = new Uri(_baseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		};

		// Set Basic authentication header
		var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
	}

	/// <summary>
	/// Set the monitor backlight brightness via xAPI command
	/// </summary>
	/// <param name="brightness">Brightness level (0-100)</param>
	/// <returns>True if successful, false otherwise</returns>
	public async Task<bool> SetBacklightAsync(int brightness)
	{
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "Brightness must be from 0 to 100.");

		try
		{
			// xAPI command: Command.Video.Output.Monitor.Backlight.Set Level: <0-100>
			var xml = $@"<Command>
  <Video>
    <Output>
      <Monitor>
        <Backlight>
          <Set>
            <Level>{brightness}</Level>
          </Set>
        </Backlight>
      </Monitor>
    </Output>
  </Video>
</Command>";

			var content = new StringContent(xml, Encoding.UTF8, "text/xml");
			var response = await _httpClient.PostAsync("/putxml", content);

			return response.IsSuccessStatusCode;
		}
		catch (Exception)
		{
			// Network errors, timeout, authentication failure, etc.
			return false;
		}
	}

	/// <summary>
	/// Get the current monitor backlight brightness via xAPI status query
	/// </summary>
	/// <returns>Brightness level (0-100) or -1 if failed</returns>
	public async Task<int> GetBacklightAsync()
	{
		try
		{
			// xAPI status query: Status/Video/Output/Monitor/Backlight/Level
			var response = await _httpClient.GetAsync("/status.xml?location=/Status/Video/Output/Monitor/Backlight");

			if (!response.IsSuccessStatusCode)
				return -1;

			var xml = await response.Content.ReadAsStringAsync();

			// Parse XML response to extract brightness level
			// Example: <Level>50</Level>
			var levelStart = xml.IndexOf("<Level>", StringComparison.OrdinalIgnoreCase);
			if (levelStart >= 0)
			{
				levelStart += 7; // Length of "<Level>"
				var levelEnd = xml.IndexOf("</Level>", levelStart, StringComparison.OrdinalIgnoreCase);
				if (levelEnd > levelStart)
				{
					var levelStr = xml.Substring(levelStart, levelEnd - levelStart).Trim();
					if (int.TryParse(levelStr, out var level) && level >= 0 && level <= 100)
					{
						return level;
					}
				}
			}

			return -1;
		}
		catch (Exception)
		{
			return -1;
		}
	}

	/// <summary>
	/// Test connection to Webex device
	/// </summary>
	/// <returns>True if device is reachable and authentication succeeds</returns>
	public async Task<bool> TestConnectionAsync()
	{
		try
		{
			// Try to get system information as a connection test
			var response = await _httpClient.GetAsync("/status.xml?location=/Status/SystemUnit");
			return response.IsSuccessStatusCode;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_httpClient?.Dispose();
		_isDisposed = true;
	}
}
