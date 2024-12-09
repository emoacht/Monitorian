using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models;

public static class OperationRecorder
{
	public static bool IsEnabled { get; private set; }

	public static async Task EnableAsync(string message)
	{
		await Logger.PrepareOperationAsync();
		await Logger.RecordOperationAsync(message);

		IsEnabled = true;
	}

	public static void Disable()
	{
		IsEnabled = false;

		FinishLineRecord();
		FinishGroupRecord();
	}

	public static Task RecordAsync(string content)
	{
		return IsEnabled
			? Logger.RecordOperationAsync(content)
			: Task.CompletedTask;
	}

	#region Line

	private static readonly Lazy<Throttle<string>> _record = new(() => new(
		TimeSpan.FromSeconds(1),
		async queue => await Logger.RecordOperationAsync(string.Join(Environment.NewLine, queue))));

	private static readonly Lazy<ConcurrentDictionary<string, List<string>>> _actionLines = new(() => new());

	/// <summary>
	/// Starts a record consisting of lines (concurrent).
	/// </summary>
	/// <param name="key">Unique key</param>
	/// <param name="actionName">Action name</param>
	public static void StartLineRecord(string key, string actionName)
	{
		if (IsEnabled)
			_actionLines.Value[key] = new List<string>([actionName]);
	}

	public static void AddLineRecord(string key, string lineString)
	{
		if (IsEnabled && _actionLines.Value.TryGetValue(key, out var lines))
			lines.Add(lineString);
	}

	public static async Task EndLineRecordAsync(string key)
	{
		if (IsEnabled && _actionLines.Value.TryGetValue(key, out var lines))
		{
			await _record.Value.PushAsync(string.Join(Environment.NewLine, lines));
			_actionLines.Value.TryRemove(key, out _);
		}
	}

	private static void FinishLineRecord()
	{
		_actionLines.Value.Clear();
	}

	#endregion

	#region Group

	private static string _actionName;
	private static readonly Lazy<List<(string groupName, StringWrapper item)>> _actionGroups = new(() => new());

	/// <summary>
	/// Starts a record consisting of groups of lines (non-concurrent).
	/// </summary>
	/// <param name="actionName">Action name</param>
	public static void StartGroupRecord(string actionName)
	{
		if (IsEnabled)
			_actionName = actionName;
	}

	public static void AddGroupRecordItem(string groupName, string itemString)
	{
		if (IsEnabled)
			_actionGroups.Value.Add((groupName, new StringWrapper(itemString)));
	}

	public static void AddGroupRecordItems(string groupName, IEnumerable<string> itemStrings)
	{
		if (IsEnabled)
			_actionGroups.Value.AddRange(itemStrings.Select(x => (groupName, new StringWrapper(x))));
	}

	public static async Task EndGroupRecordAsync()
	{
		if (IsEnabled)
		{
			var groupsStrings = _actionGroups.Value.GroupBy(x => x.groupName).Select(x => (x.Key, (object)x.Select(y => y.item))).ToArray();

			await Logger.RecordOperationAsync($"{_actionName}{Environment.NewLine}{SimpleSerialization.Serialize(groupsStrings)}");
			FinishGroupRecord();
		}
	}

	private static void FinishGroupRecord()
	{
		_actionName = null;
		_actionGroups.Value.Clear();
	}

	#endregion
}