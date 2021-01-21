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
		public OperationRecorder(string message) => LogService.RecordOperation(message);

		public void Record(string content) => LogService.RecordOperation(content);

		#region Line

		private readonly ConcurrentDictionary<string, List<string>> _actionLines = new();

		/// <summary>
		/// Starts a record consisting of lines (concurrent).
		/// </summary>
		/// <param name="key">Unique key</param>
		/// <param name="actionName">Action name</param>
		public void StartLineRecord(string key, string actionName)
		{
			_actionLines[key] = new List<string>(new[] { actionName });
		}

		public void AddLineRecord(string key, string lineString)
		{
			if (_actionLines.TryGetValue(key, out var lines))
				lines.Add(lineString);
		}

		public void EndLineRecord(string key)
		{
			if (_actionLines.TryGetValue(key, out var lines))
			{
				LogService.RecordOperation(string.Join(Environment.NewLine, lines));

				_actionLines.TryRemove(key, out _);
			}
		}

		#endregion

		#region Group

		private string _actionName;
		private readonly List<(string groupName, StringWrapper item)> _actionGroups = new();

		/// <summary>
		/// Starts a record consisting of groups of lines (non-concurrent).
		/// </summary>
		/// <param name="actionName">Action name</param>
		public void StartGroupRecord(string actionName) => this._actionName = actionName;

		public void AddGroupRecordItem(string groupName, string itemString) =>
			_actionGroups.Add((groupName, new StringWrapper(itemString)));

		public void AddGroupRecordItems(string groupName, IEnumerable<string> itemStrings) =>
			_actionGroups.AddRange(itemStrings.Select(x => (groupName, new StringWrapper(x))));

		public void EndGroupRecord()
		{
			var groupsStrings = _actionGroups.GroupBy(x => x.groupName).Select(x => (x.Key, (object)x.Select(y => y.item))).ToArray();

			LogService.RecordOperation($"{_actionName}{Environment.NewLine}{SimpleSerialization.Serialize(groupsStrings)}");

			_actionName = null;
			_actionGroups.Clear();
		}

		#endregion
	}
}