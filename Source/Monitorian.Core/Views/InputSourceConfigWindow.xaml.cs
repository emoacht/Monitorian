using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;

namespace Monitorian.Core.Views;

public partial class InputSourceConfigWindow : Window
{
	private readonly MonitorViewModel _viewModel;
	private readonly ObservableCollection<InputSourceConfigItem> _items;

	public InputSourceConfigWindow(MonitorViewModel viewModel)
	{
		InitializeComponent();

		_viewModel = viewModel;
		_items = new ObservableCollection<InputSourceConfigItem>();

		// Make sure we have the latest available inputs
		_viewModel.UpdateInputSource();

		// Populate the list with available input sources
		var availableInputs = _viewModel.AvailableInputSources ?? System.Array.Empty<byte>();
		var configuredInputs = _viewModel.InputSources ?? System.Array.Empty<InputSourceItem>();

		foreach (var value in availableInputs)
		{
			var configured = configuredInputs.FirstOrDefault(x => x.Value == value);
			var item = new InputSourceConfigItem
			{
				Value = value,
				Label = configured?.Label ?? InputSourceItem.GetDefaultLabel(value),
				IsEnabled = configured?.IsEnabled ?? true
			};
			_items.Add(item);
		}

		InputSourcesList.ItemsSource = _items;
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		// Convert to InputSourceItem array and save
		var inputSources = _items
			.Select(x => new InputSourceItem(x.Value, x.Label, x.IsEnabled))
			.ToArray();

		_viewModel.InputSources = inputSources;
		DialogResult = true;
		Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
		Close();
	}
}

/// <summary>
/// Configuration item for the UI
/// </summary>
public class InputSourceConfigItem : INotifyPropertyChanged
{
	public byte Value { get; set; }

	public string ValueHex => $"0x{Value:X2}";

	private string _label;
	public string Label
	{
		get => _label;
		set
		{
			if (_label != value)
			{
				_label = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
			}
		}
	}

	private bool _isEnabled;
	public bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (_isEnabled != value)
			{
				_isEnabled = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}
