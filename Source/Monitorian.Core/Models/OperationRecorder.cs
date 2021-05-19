using System;
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
		private OperationRecorder()
		{ }

		public static async Task<OperationRecorder> CreateAsync(string message)
		{
			await Logger.PrepareOperationAsync();
			await Logger.RecordOperationAsync(message);

			return new();
		}

		public Task RecordAsync(string content) => Logger.RecordOperationAsync(content);

		#region Line

		private readonly Lazy<Throttle<string>> _record = new(() => new(
			TimeSpan.FromSeconds(1),
			async queue => await Logger.RecordOperationAsync(string.Join(Environment.NewLine, queue))));

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

			await Logger.RecordOperationAsync($"{_actionName}{Environment.NewLine}{SimpleSerialization.Serialize(groupsStrings)}");

			_actionName = null;
			_actionGroups.Value.Clear();
		}

		#endregion
	}
}