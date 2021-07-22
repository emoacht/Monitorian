﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Collections;
using Monitorian.Core.Common;
using Monitorian.Core.Helper;

namespace Monitorian.Core.Models
{
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
			set => SetPropertyValue(ref _usesLargeElements, value);
		}
		private bool _usesLargeElements = true; // Default

		/// <summary>
		/// Whether to show adjusted brightness
		/// </summary>
		[DataMember]
		public bool ShowsAdjusted
		{
			get => _showsAdjusted;
			set => SetPropertyValue(ref _showsAdjusted, value);
		}
		private bool _showsAdjusted = true; // default

		/// <summary>
		/// Whether to show current number
		/// </summary>
		[DataMember]
		public bool ShowsNumber
		{
			get => _showsNumber;
			set => SetPropertyValue(ref _showsNumber, value);
		}
		private bool _showsNumber = true; // default

		/// <summary>
		/// Whether to order by monitors arrangement
		/// </summary>
		[DataMember]
		public bool OrdersArrangement
		{
			get => _ordersArrangement;
			set => SetPropertyValue(ref _ordersArrangement, value);
		}
		private bool _ordersArrangement = true; // default

		/// <summary>
		/// Whether to defer change until stopped
		/// </summary>
		[DataMember]
		public bool DefersChange
		{
			get => _defersChange;
			set => SetPropertyValue(ref _defersChange, value);
		}
		private bool _defersChange;

		/// <summary>
		/// Whether to enable moving in unison
		/// </summary>
		[DataMember]
		public bool EnablesUnison
		{
			get => _enablesUnison;
			set => SetPropertyValue(ref _enablesUnison, value);
		}
		private bool _enablesUnison;

		/// <summary>
		/// Whether to enable changing adjustable range
		/// </summary>
		[DataMember]
		public bool EnablesRange
		{
			get => _enablesRange;
			set => SetPropertyValue(ref _enablesRange, value);
		}
		private bool _enablesRange;

		/// <summary>
		/// Whether to enable changing contrast
		/// </summary>
		[DataMember]
		public bool EnablesContrast
		{
			get => _enablesContrast;
			set => SetPropertyValue(ref _enablesContrast, value);
		}
		private bool _enablesContrast;

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
			set => SetPropertyValue(ref _selectedDeviceInstanceId, value);
		}
		private string _selectedDeviceInstanceId;

		/// <summary>
		/// Whether to make operation log
		/// </summary>
		[DataMember]
		public bool MakesOperationLog
		{
			get => _makesOperationLog;
			set => SetPropertyValue(ref _makesOperationLog, value);
		}
		private bool _makesOperationLog;

		#endregion

		protected Type[] KnownTypes { get; set; }

		private const string SettingsFileName = "settings.xml";
		private readonly string _fileName;

		public SettingsCore() : this(null)
		{ }

		protected SettingsCore(string fileName)
		{
			this._fileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : SettingsFileName;
		}

		private Throttle _save;

		protected internal virtual async Task InitiateAsync()
		{
			await Task.Run(() => Load(this));

			_save = new Throttle(
				TimeSpan.FromMilliseconds(100),
				() => Save(this));

			MonitorCustomizations.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(MonitorCustomizations));
			PropertyChanged += async (sender, e) => await _save.PushAsync();
		}

		#region Load/Save

		private void Load<T>(T instance) where T : class
		{
			try
			{
				AppDataService.Load(instance, _fileName, KnownTypes);
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
				AppDataService.Save(instance, _fileName, KnownTypes);
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

		public MonitorCustomizationItem(string name, bool isUnison)
		{
			this.Name = name;
			this.IsUnison = isUnison;
		}

		public MonitorCustomizationItem(string name, bool isUnison, byte lowest, byte highest) : this(name, isUnison)
		{
			this.Lowest = lowest;
			this.Highest = highest;
		}
	}
}