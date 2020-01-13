using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models
{
	internal class OperationRecorder
	{
		public OperationRecorder(string message) => LogService.RecordOperation(message);

		public void Record(string content) => LogService.RecordOperation(content);

		private string _actionName;

		public void StartRecord(string actionName) => this._actionName = actionName;

		private readonly List<(string group, StringWrapper item)> _groups = new List<(string, StringWrapper)>();

		public void AddItem(string groupName, string itemString) => _groups.Add((groupName, new StringWrapper(itemString)));
		public void AddItems(string groupName, IEnumerable<string> itemStrings) => _groups.AddRange(itemStrings.Select(x => (groupName, new StringWrapper(x))));

		public void StopRecord()
		{
			var groupsStrings = _groups.GroupBy(x => x.group).Select(x => (x.Key, (object)x.Select(y => y.item))).ToArray();

			LogService.RecordOperation($"{_actionName}{Environment.NewLine}{SimpleSerialization.Serialize(groupsStrings)}");

			_groups.Clear();
		}
	}
}