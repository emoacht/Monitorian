using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Monitorian.Core.Collections;
using Monitorian.Core.Common;
using Monitorian.Core.Helper;
using Monitorian.Core.Views;

namespace Monitorian.Core.Models;

/// <summary>
/// Settings
/// </summary>
[DataContract]
public class SettingsCore : BindableBase
{
	#region Settings (persistent)

	/// <summary>
	/// Whether to use large elements
	/// </summary>
	[DataMember]
	public bool UsesLargeElements
	{
		get => _usesLargeElements;
		set => SetProperty(ref _usesLargeElements, value);
	}
	private bool _usesLargeElements = true; // Default

	/// <summary>
	/// Whether to use accent color for brightness
	/// </summary>
	[DataMember]
	public bool UsesAccentColor
	{
		get => _usesAccentColor;
		set => SetProperty(ref _usesAccentColor, value);
	}
	private bool _usesAccentColor;

	/// <summary>
	/// Whether to show adjusted brightness
	/// </summary>
	[DataMember]
	public bool ShowsAdjusted
	{
		get => _showsAdjusted;
		set => SetProperty(ref _showsAdjusted, value);
	}
	private bool _showsAdjusted = true; // default

	/// <summary>
	/// Whether to order by monitors arrangement
	/// </summary>
	[DataMember]
	public bool OrdersArrangement
	{
		get => _ordersArrangement;
		set => SetProperty(ref _ordersArrangement, value);
	}
	private bool _ordersArrangement = true; // default

	/// <summary>
	/// Whether to defer change until stopped
	/// </summary>
	[DataMember]
	public bool DefersChange
	{
		get => _defersChange;
		set => SetProperty(ref _defersChange, value);
	}
	private bool _defersChange;

	/// <summary>
	/// Whether to adjust SDR content brightness
	/// </summary>
	[DataMember]
	public bool AdjustsSdrContent
	{
		get => _adjustsSdrContent;
		set => SetProperty(ref _adjustsSdrContent, value);
	}
	private bool _adjustsSdrContent;

	/// <summary>
	/// Whether to invert scroll direction
	/// </summary>
	/// <remarks>
	/// This value is a set of flags.
	/// </remarks>
	public ScrollInput InvertsScrollDirection
	{
		get => _invertsScrollDirection ??= (ScrollInput)0b_1110; // default
		set => SetProperty(ref _invertsScrollDirection, value);
	}
	[DataMember(Name = nameof(InvertsScrollDirection))]
	private ScrollInput? _invertsScrollDirection;

	/// <summary>
	/// Whether to enable moving in unison
	/// </summary>
	[DataMember]
	public bool EnablesUnison
	{
		get => _enablesUnison;
		set => SetProperty(ref _enablesUnison, value);
	}
	private bool _enablesUnison;

	/// <summary>
	/// Whether to enable changing adjustable range
	/// </summary>
	[DataMember]
	public bool EnablesRange
	{
		get => _enablesRange;
		set => SetProperty(ref _enablesRange, value);
	}
	private bool _enablesRange;

	/// <summary>
	/// Whether to enable changing contrast
	/// </summary>
	[DataMember]
	public bool EnablesContrast
	{
		get => _enablesContrast;
		set => SetProperty(ref _enablesContrast, value);
	}
	private bool _enablesContrast;

	/// <summary>
	/// Whether to enable input source switching
	/// </summary>
	[DataMember]
	public bool EnablesInputSource
	{
		get => _enablesInputSource;
		set => SetProperty(ref _enablesInputSource, value);
	}
	private bool _enablesInputSource;

	/// <summary>
	/// Monitor customizations by user
	/// </summary>
	[DataMember]
	public ObservableKeyedList<string, MonitorCustomizationItem> MonitorCustomizations
	{
		get => _monitorCustomizations ??= new ObservableKeyedList<string, MonitorCustomizationItem>();
		protected set => _monitorCustomizations = value;
	}
	private ObservableKeyedList<string, MonitorCustomizationItem> _monitorCustomizations;

	/// <summary>
	/// Device Instance ID of selected monitor
	/// </summary>
	[DataMember]
	public string SelectedDeviceInstanceId
	{
		get => _selectedDeviceInstanceId;
		set => SetProperty(ref _selectedDeviceInstanceId, value);
	}
	private string _selectedDeviceInstanceId;

	/// <summary>
	/// Whether to record operations to log
	/// </summary>
	[DataMember]
	public bool RecordsOperationLog
	{
		get => _recordsOperationLog;
		set => SetProperty(ref _recordsOperationLog, value);
	}
	private bool _recordsOperationLog;

	#endregion

	protected Type[] KnownTypes { get; set; }

	private const string SettingsFileName = "settings.xml";

	protected string FileName
	{
		get => _fileName;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
				_fileName = value;
		}
	}
	private string _fileName = SettingsFileName;

	public SettingsCore()
	{ }

	private Throttle _save;

	protected internal virtual async Task InitiateAsync()
	{
		await Task.Run(() => Load(this));

		_save = new Throttle(
			TimeSpan.FromMilliseconds(100),
			() => Save(this));

		MonitorCustomizations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(MonitorCustomizations));
		PropertyChanged += async (_, _) => await _save.PushAsync();
	}

	#region Load/Save

	private void Load<T>(T instance) where T : class
	{
		try
		{
			AppDataService.Load(instance, FileName, KnownTypes);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to load settings from AppData." + Environment.NewLine
				+ ex);
		}
	}

	private void Save<T>(T instance) where T : class
	{
		try
		{
			AppDataService.Save(instance, FileName, KnownTypes);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to save settings to AppData." + Environment.NewLine
				+ ex);
		}
	}

	#endregion
}

[DataContract]
public class MonitorCustomizationItem
{
	[DataMember]
	public string Name { get; private set; }

	[DataMember(Name = "Unison")]
	public bool IsUnison { get; private set; }

	[DataMember]
	public byte Lowest { get; private set; } = 0;

	[DataMember]
	public byte Highest { get; private set; } = 100;

	/// <summary>
	/// Configured input sources with their labels (VCP code 0x60 values)
	/// </summary>
	[DataMember]
	public InputSourceItem[] InputSources { get; private set; }

	public MonitorCustomizationItem(string name, bool isUnison, byte lowest, byte highest, InputSourceItem[] inputSources = null)
	{
		this.Name = name;
		this.IsUnison = isUnison;
		this.Lowest = lowest;
		this.Highest = highest;
		this.InputSources = inputSources;
	}

	internal bool IsValid
	{
		get => (Lowest < Highest) && (Highest <= 100);
	}

	internal bool IsDefault
	{
		get => (Name is null)
			&& (IsUnison == default)
			&& (Lowest, Highest) is (0, 100)
			&& (InputSources is null || InputSources.Length == 0);
	}
}

/// <summary>
/// Input source configuration item
/// </summary>
[DataContract]
public class InputSourceItem
{
	/// <summary>
	/// VCP code 0x60 value for this input source
	/// </summary>
	[DataMember]
	public byte Value { get; set; }

	/// <summary>
	/// User-defined label for this input source
	/// </summary>
	[DataMember]
	public string Label { get; set; }

	/// <summary>
	/// Whether this input is enabled for quick switching
	/// </summary>
	[DataMember]
	public bool IsEnabled { get; set; }

	public InputSourceItem() { }

	public InputSourceItem(byte value, string label, bool isEnabled = true)
	{
		this.Value = value;
		this.Label = label;
		this.IsEnabled = isEnabled;
	}

	/// <summary>
	/// Gets the default label for an input source VCP value
	/// </summary>
	public static string GetDefaultLabel(byte value)
	{
		return value switch
		{
			0x01 => "VGA 1",
			0x02 => "VGA 2",
			0x03 => "DVI 1",
			0x04 => "DVI 2",
			0x05 => "Composite 1",
			0x06 => "Composite 2",
			0x07 => "S-Video 1",
			0x08 => "S-Video 2",
			0x09 => "Tuner 1",
			0x0A => "Tuner 2",
			0x0B => "Tuner 3",
			0x0C => "Component 1",
			0x0D => "Component 2",
			0x0E => "Component 3",
			0x0F => "DisplayPort 1",
			0x10 => "DisplayPort 2",
			0x11 => "HDMI 1",
			0x12 => "HDMI 2",
			0x13 => "USB-C 1",
			0x14 => "USB-C 2",
			_ => $"Input {value:X2}"
		};
	}
}