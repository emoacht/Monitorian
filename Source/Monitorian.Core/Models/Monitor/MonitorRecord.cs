using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor;

internal class MonitorRecord
{
	public static int Capacity { get; set; } = 16;

	[DataContract]
	internal class MonitorRecordItem(float minimum, float maximum, long accessTimeTicks)
	{
		[DataMember]
		public float Minimum { get; private set; } = minimum;

		[DataMember]
		public float Maximum { get; private set; } = maximum;

		[DataMember]
		public long AccessTimeTicks { get; set; } = accessTimeTicks;
	}

	private static Dictionary<string, MonitorRecordItem> _records = [];
	private static readonly ReaderWriterLockSlim _lock = new();

	public static bool TryRead(string deviceInstanceId, out float minimum, out float maximum)
	{
		try
		{
			_lock.EnterReadLock();

			if (_records.TryGetValue(deviceInstanceId, out var item))
			{
				(minimum, maximum) = (item.Minimum, item.Maximum);
				item.AccessTimeTicks = DateTimeOffset.UtcNow.Ticks;
				return true;
			}
			(minimum, maximum) = (0, 0);
			return false;
		}
		finally
		{
			if (_lock.IsReadLockHeld)
				_lock.ExitReadLock();
		}
	}

	public static void Write(string deviceInstanceId, float minimum, float maximum)
	{
		if (minimum >= maximum)
			return;

		try
		{
			_lock.EnterWriteLock();

			_records[deviceInstanceId] = new MonitorRecordItem(minimum, maximum, DateTimeOffset.UtcNow.Ticks);
		}
		finally
		{
			if (_lock.IsWriteLockHeld)
				_lock.ExitWriteLock();
		}
	}

	protected const string MonitorsFileName = "record.json";

	protected internal static async Task InitiateAsync()
	{
		try
		{
			var content = await AppDataService.ReadAsync(MonitorsFileName);
			if (!string.IsNullOrEmpty(content))
			{
				using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
				var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, MonitorRecordItem>));
				_records = (Dictionary<string, MonitorRecordItem>)serializer.ReadObject(ms);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to load monitor records from AppData." + Environment.NewLine
				+ ex);
		}

		Debug.Assert(Capacity > 0);
		if (_records.Count > Capacity)
		{
			var ids = _records
				.OrderBy(x => x.Value.AccessTimeTicks)
				.Take(_records.Count - Capacity)
				.Select(x => x.Key)
				.ToArray();

			foreach (var id in ids)
				_records.Remove(id);
		}
	}

	protected internal static void End()
	{
		try
		{
			string content = null;
			if (_records.Count > 0)
			{
				using var ms = new MemoryStream();
				using var jw = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true);
				var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, MonitorRecordItem>));
				serializer.WriteObject(jw, _records);
				jw.Flush();
				content = Encoding.UTF8.GetString(ms.ToArray());
			}
			AppDataService.Write(MonitorsFileName, false, true, content);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to save monitor records to AppData." + Environment.NewLine
				+ ex);
		}
	}
}