using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Watcher
{
	public interface ICountEventArgs
	{
		int Count { get; }
		string Description { get; }
	}

	public class CountEventArgs<T> : EventArgs, ICountEventArgs
	{
		public T Data { get; }
		public int Count { get; }
		public string Description => $" {Data} {Count}";

		public CountEventArgs(T data, int count) => (this.Data, this.Count) = (data, count);
	}
}