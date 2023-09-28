using System;

namespace Monitorian.Core.Models.Watcher;

public interface ICountEventArgs
{
	int Count { get; }
	string Description { get; }
}

public class CountEventArgs : EventArgs, ICountEventArgs
{
	public int Count { get; }
	public virtual string Description => $" {Count}";

	public CountEventArgs(int count) => this.Count = count;
}

public class CountEventArgs<T> : EventArgs, ICountEventArgs
{
	public T Data { get; }
	public int Count { get; }
	public string Description => $" {Data} {Count}";

	public CountEventArgs(T data, int count) => (this.Data, this.Count) = (data, count);
}