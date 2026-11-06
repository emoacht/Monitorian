using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models;

public static class OperationRecorder
{
	public static bool IsEnabled { get; private set; }

	public static async Task EnableAsync(string message)
	{
		await Logger.PrepareOperationAsync();
		await Logger.SaveOperationAsync(message, DateTime.Now);

		StartQueue();
		IsEnabled = true;
	}

	public static void Disable()
	{
		IsEnabled = false;
		StopQueue();

		StopFlowRecord();
		StopGroupRecord();
	}

	public static void Record(string content) => Record(content, DateTime.Now);
	private static void Record(string content, DateTime time) => PushQueue(content, time);

	#region Queue

	private class QueueWorker
	{
		private readonly ConcurrentQueue<(string content, DateTime time)> _queue = new();
		private readonly SemaphoreSlim _semaphore = new(0);
		private readonly CancellationTokenSource _cts = new();

		public QueueWorker()
		{
			Task.Run(() => WriterLoopAsync(_cts.Token)); // fire-and-forget
		}

		public void Push(string content, DateTime time)
		{
			if (_cts.IsCancellationRequested ||
				(_semaphore.CurrentCount >= 100))
				return;

			_queue.Enqueue((content, time));
			_semaphore.Release();
		}

		private async Task WriterLoopAsync(CancellationToken token)
		{
			try
			{
				while (true)
				{
					await _semaphore.WaitAsync(token).ConfigureAwait(false);

					if (_queue.TryDequeue(out var item))
						await Logger.SaveOperationAsync(item.content, item.time).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine($"Stopped queue worker.");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to loop queue worker." + Environment.NewLine
					+ ex);
			}
		}

		public void End() => _cts.Cancel();
	}

	private static QueueWorker _queueWorker;

	private static void StartQueue()
	{
		_queueWorker = new QueueWorker();
	}

	private static void PushQueue(string content, DateTime time)
	{
		_queueWorker?.Push(content, time);
	}

	private static void StopQueue()
	{
		_queueWorker?.End();
		_queueWorker = null;
	}

	#endregion

	#region Flow

	private static DateTime? _flowRecordsStartTime;
	private static readonly Lazy<ConcurrentDictionary<string, List<string>>> _flowRecords = new(() => new());

	private static readonly Lazy<Throttle<string>> _flowRecordsRecord = new(() => new(
		TimeSpan.FromSeconds(1),
		(queue) =>
		{
			var buffer = _flowRecordsStartTime.GetValueOrDefault();
			_flowRecordsStartTime = null;
			Record(string.Join(Environment.NewLine, queue), buffer);
		}));

	/// <summary>
	/// Starts a record consisting of flows of items (concurrent).
	/// </summary>
	/// <param name="key">Unique key</param>
	/// <param name="actionName">Action name</param>
	public static void StartFlowRecord(string key, string actionName)
	{
		if (IsEnabled)
		{
			_flowRecordsStartTime ??= DateTime.Now;
			_flowRecords.Value[key] = new List<string>([actionName]);
		}
	}

	public static void AddFlowRecord(string key, string item)
	{
		if (IsEnabled && _flowRecords.Value.TryGetValue(key, out var items))
			items.Add(item);
	}

	public static void FlushFlowRecord(string key)
	{
		if (IsEnabled && _flowRecords.Value.TryGetValue(key, out var items))
		{
			_flowRecordsRecord.Value.Push(string.Join(Environment.NewLine, items));
			_flowRecords.Value.TryRemove(key, out _); // Cleanup
		}
	}

	private static void StopFlowRecord()
	{
		_flowRecords.Value.Clear();
	}

	#endregion

	#region Group

	private static string _groupActionName;
	private static DateTime _groupRecordsStartTime;
	private static List<(string groupName, object item)> _groupRecords;

	/// <summary>
	/// Starts a record consisting of groups of items (non-concurrent).
	/// </summary>
	/// <param name="actionName">Action name</param>
	public static void StartGroupRecord(string actionName)
	{
		if (IsEnabled)
		{
			_groupActionName = actionName;
			_groupRecordsStartTime = DateTime.Now;
			_groupRecords = new List<(string, object)>();
		}
	}

	public static void AddGroupRecordItem(string groupName, object item)
	{
		if (IsEnabled)
			_groupRecords?.Add((groupName, item));
	}

	public static void AddGroupRecordItems(string groupName, IEnumerable<object> items)
	{
		if (IsEnabled)
			_groupRecords?.AddRange(items.Select(x => (groupName, x)));
	}

	public static void FlushGroupRecord()
	{
		if (IsEnabled && (_groupRecords is { Count: > 0 }))
		{
			var buffer = new StringBuilder(_groupActionName);
			buffer.AppendLine($" ({(DateTime.Now - _groupRecordsStartTime).TotalSeconds:F3})");

			var groupObjects = _groupRecords
				.GroupBy(x => x.groupName)
				.Select(x => (x.Key, (object)x.Select(y => y.item)))
				.ToArray();
			buffer.Append(SimpleSerialization.Serialize(groupObjects));

			Record(buffer.ToString(), _groupRecordsStartTime);
			StopGroupRecord(); // Cleanup
		}
	}

	private static void StopGroupRecord()
	{
		_groupActionName = null;
		_groupRecordsStartTime = default;
		_groupRecords = null;
	}

	#endregion
}