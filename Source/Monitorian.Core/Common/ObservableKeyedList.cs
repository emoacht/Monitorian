using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Common
{
	/// <summary>
	/// A wrapper class of <see cref="System.Collections.Generic.IEnumerable{KeyValuePair{TKey, TValue}}"/> which implements INotifyCollectionChanged
	/// </summary>
	/// <typeparam name="TKey">Type of keys</typeparam>
	/// <typeparam name="TValue">Type of values</typeparam>
	/// <remarks>The last accessed item will be automatically moved to the head.</remarks>
	[DataContract]
	public class ObservableKeyedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		[DataMember(Name = "InnerList")]
		protected List<KeyValuePair<TKey, TValue>> List
		{
			get => _list ??= new List<KeyValuePair<TKey, TValue>>();
			private set => _list = value;
		}
		private List<KeyValuePair<TKey, TValue>> _list;

		public int Count => List.Count;
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => List.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

		protected object Lock => _lock ??= new object();
		private object _lock;

		/// <summary>
		/// Absolute capacity of the list
		/// </summary>
		/// <remarks>If set, the overflown item at the tail will be automatically removed.</remarks>
		public int AbsoluteCapacity
		{
			get => _absoluteCapacity;
			set => _absoluteCapacity = Math.Max(0, value);
		}
		[field: NonSerialized]
		private int _absoluteCapacity = int.MaxValue;

		private bool TryFindIndex(TKey key, out int index)
		{
			index = List.FindIndex(x => EqualityComparer<TKey>.Default.Equals(x.Key, key));
			return (0 <= index);
		}

		private void Reorder(KeyValuePair<TKey, TValue> item, int oldIndex)
		{
			if (oldIndex <= 0)
				return;

			List.RemoveAt(oldIndex);
			List.Insert(0, item);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
				action: NotifyCollectionChangedAction.Move,
				changedItem: item,
				index: 0,
				oldIndex: oldIndex));
		}

		private void Truncate()
		{
			while ((1 <= List.Count) && (AbsoluteCapacity < List.Count))
			{
				var item = List[List.Count - 1];
				List.RemoveAt(List.Count - 1);
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					action: NotifyCollectionChangedAction.Remove,
					changedItem: item));
			}
		}

		public bool ContainsKey(TKey key)
		{
			lock (Lock)
			{
				return TryFindIndex(key, out int _);
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				lock (Lock)
				{
					if (!TryFindIndex(key, out int index))
						throw new KeyNotFoundException("The given key does not exist in the list.");

					var item = List[index];
					Reorder(item, index);
					return item.Value;
				}
			}
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			lock (Lock)
			{
				if (!TryFindIndex(key, out int index))
				{
					value = default;
					return false;
				}

				var item = List[index];
				Reorder(item, index);
				value = item.Value;
				return true;
			}
		}

		public void Add(TKey key, TValue value)
		{
			lock (Lock)
			{
				var newItem = new KeyValuePair<TKey, TValue>(key, value);

				if (!TryFindIndex(key, out int index))
				{
					List.Insert(0, newItem);
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
						action: NotifyCollectionChangedAction.Add,
						changedItem: newItem));
					Truncate();
				}
				else
				{
					var oldItem = List[index];
					List.RemoveAt(index);
					List.Insert(0, newItem);
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
						action: NotifyCollectionChangedAction.Replace,
						newItem: newItem,
						oldItem: oldItem));
				}
			}
		}

		public bool Remove(TKey key)
		{
			lock (Lock)
			{
				if (!TryFindIndex(key, out int index))
					return false;

				var oldItem = List[index];
				List.RemoveAt(index);
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					action: NotifyCollectionChangedAction.Remove,
					changedItem: oldItem));
				return true;
			}
		}

		public void Clear()
		{
			lock (Lock)
			{
				List.Clear();
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					action: NotifyCollectionChangedAction.Reset));
			}
		}
	}
}