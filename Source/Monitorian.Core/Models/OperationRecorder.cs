﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models
{
	public class OperationRecorder
	{
		/// <summary>
		/// The number of entries that operation log file can contain
		/// </summary>
		public static int Capacity { get; set; } = 128;

		public OperationRecorder(string message) => LogService.RecordOperation(message, Capacity);

		public void Record(string content) => LogService.RecordOperation(content, Capacity);

		#region Line

		private readonly Lazy<Throttle<string>> _record = new(() => new(
			TimeSpan.FromSeconds(1),
			async queue => await Task.Run(() => LogService.RecordOperation(string.Join(Environment.NewLine, queue), Capacity))));

		private readonly Lazy<ConcurrentDictionary<string, List<string>>> _actionLines = new(() => new());

		/// <summary>
		/// Starts a record consisting of lines (concurrent).
		/// </summary>
		/// <param name="key">Unique key</param>
		/// <param name="actionName">Action name</param>
		public void StartLineRecord(string key, string actionName)
		{
			_actionLines.Value[key] = new List<string>(new[] { actionName });
		}

		public void AddLineRecord(string key, string lineString)
		{
			if (_actionLines.Value.TryGetValue(key, out var lines))
				lines.Add(lineString);
		}

		public async Task EndLineRecordAsync(string key)
		{
			if (_actionLines.Value.TryGetValue(key, out var lines))
			{
				await _record.Value.PushAsync(string.Join(Environment.NewLine, lines));
				_actionLines.Value.TryRemove(key, out _);
			}
		}

		#endregion

		#region Group

		private string _actionName;
		private readonly Lazy<List<(string groupName, StringWrapper item)>> _actionGroups = new(() => new());

		/// <summary>
		/// Starts a record consisting of groups of lines (non-concurrent).
		/// </summary>
		/// <param name="actionName">Action name</param>
		public void StartGroupRecord(string actionName) => this._actionName = actionName;

		public void AddGroupRecordItem(string groupName, string itemString) =>
			_actionGroups.Value.Add((groupName, new StringWrapper(itemString)));

		public void AddGroupRecordItems(string groupName, IEnumerable<string> itemStrings) =>
			_actionGroups.Value.AddRange(itemStrings.Select(x => (groupName, new StringWrapper(x))));

		public async Task EndGroupRecordAsync()
		{
			var groupsStrings = _actionGroups.Value.GroupBy(x => x.groupName).Select(x => (x.Key, (object)x.Select(y => y.item))).ToArray();

			await Task.Run(() => LogService.RecordOperation($"{_actionName}{Environment.NewLine}{SimpleSerialization.Serialize(groupsStrings)}", Capacity));

			_actionName = null;
			_actionGroups.Value.Clear();
		}

		#endregion
	}
}