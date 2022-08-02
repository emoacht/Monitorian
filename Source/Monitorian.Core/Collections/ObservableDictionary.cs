using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Collections
{
	/// <summary>
	/// A wrapper class of <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> which implements INotifyCollectionChanged
	/// </summary>
	/// <typeparam name="TKey">Type of keys</typeparam>
	/// <typeparam name="TValue">Type of values</typeparam>
	[DataContract]
	public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		[DataMember(Name = "InnerDictionary")]
		protected IDictionary<TKey, TValue> Dictionary
		{
			get => _dictionary ??= new Dictionary<TKey, TValue>();
			private set => _dictionary = value;
		}
		private IDictionary<TKey, TValue> _dictionary;

		public ICollection<TKey> Keys => Dictionary.Keys;
		public ICollection<TValue> Values => Dictionary.Values;
		public int Count => Dictionary.Count;
		public bool IsReadOnly => Dictionary.IsReadOnly;
		public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);
		public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
		public bool Contains(KeyValuePair<TKey, TValue> item) => Dictionary.Contains(item);
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => Dictionary.CopyTo(array, arrayIndex);
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Dictionary.GetEnumerator();

		protected object Lock => _lock ??= new object();
		private object _lock;

		public virtual TValue this[TKey key]
		{
			get
			{
				lock (Lock)
				{
					return Dictionary[key];
				}
			}
			set
			{
				lock (Lock)
				{
					if (!Dictionary.TryGetValue(key, out TValue oldValue))
					{
						Add(key, value);
						return;
					}

					if (EqualityComparer<TValue>.Default.Equals(value, oldValue))
						return;

					Dictionary[key] = value;
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
						action: NotifyCollectionChangedAction.Replace,
						newItem: new KeyValuePair<TKey, TValue>(key, value),
						oldItem: new KeyValuePair<TKey, TValue>(key, oldValue)));
				}
			}
		}

		public virtual void Add(TKey key, TValue value)
		{
			lock (Lock)
			{
				Add(new KeyValuePair<TKey, TValue>(key, value));
			}
		}

		public virtual void Add(KeyValuePair<TKey, TValue> item)
		{
			lock (Lock)
			{
				Dictionary.Add(item);
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					action: NotifyCollectionChangedAction.Add,
					changedItem: item));
			}
		}

		public virtual bool Remove(TKey key)
		{
			lock (Lock)
			{
				if (!Dictionary.TryGetValue(key, out TValue value))
					return false;

				return Remove(new KeyValuePair<TKey, TValue>(key, value));
			}
		}

		public virtual bool Remove(KeyValuePair<TKey, TValue> item)
		{
			lock (Lock)
			{
				int index = Dictionary.Keys.ToList().IndexOf(item.Key);
				if (index < 0)
					return false;

				Dictionary.Remove(item);

				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					action: NotifyCollectionChangedAction.Remove,
					changedItem: item,
					index: index));
				return true;
			}
		}

		public virtual void Clear()
		{
			lock (Lock)
			{
				Dictionary.Clear();
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					action: NotifyCollectionChangedAction.Reset));
			}
		}
	}
}