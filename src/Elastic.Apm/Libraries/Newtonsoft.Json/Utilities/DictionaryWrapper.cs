﻿#region License

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if !HAVE_LINQ
using Elastic.Apm.Libraries.Newtonsoft.Json.Utilities.LinqBridge;

#else
using System.Linq;
#endif

#nullable enable
namespace Elastic.Apm.Libraries.Newtonsoft.Json.Utilities
{
	internal interface IWrappedDictionary
		: IDictionary
	{
		object UnderlyingDictionary { get; }
	}

	internal class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>, IWrappedDictionary
	{
		private readonly IDictionary? _dictionary;
		private readonly IDictionary<TKey, TValue>? _genericDictionary;
#if HAVE_READ_ONLY_COLLECTIONS
        private readonly IReadOnlyDictionary<TKey, TValue>? _readOnlyDictionary;
#endif
		private object? _syncRoot;

		public DictionaryWrapper(IDictionary dictionary)
		{
			ValidationUtils.ArgumentNotNull(dictionary, nameof(dictionary));

			_dictionary = dictionary;
		}

		public DictionaryWrapper(IDictionary<TKey, TValue> dictionary)
		{
			ValidationUtils.ArgumentNotNull(dictionary, nameof(dictionary));

			_genericDictionary = dictionary;
		}

#if HAVE_READ_ONLY_COLLECTIONS
        public DictionaryWrapper(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            ValidationUtils.ArgumentNotNull(dictionary, nameof(dictionary));

            _readOnlyDictionary = dictionary;
        }
#endif

		internal IDictionary<TKey, TValue> GenericDictionary
		{
			get
			{
				MiscellaneousUtils.Assert(_genericDictionary != null);
				return _genericDictionary;
			}
		}

		public void Add(TKey key, TValue value)
		{
			if (_dictionary != null)
				_dictionary.Add(key, value);
			else if (_genericDictionary != null)
				_genericDictionary.Add(key, value);
			else
				throw new NotSupportedException();
		}

		public bool ContainsKey(TKey key)
		{
			if (_dictionary != null)
				return _dictionary.Contains(key);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                return _readOnlyDictionary.ContainsKey(key);
            }
#endif

			return GenericDictionary.ContainsKey(key);
		}

		public ICollection<TKey> Keys
		{
			get
			{
				if (_dictionary != null)
					return _dictionary.Keys.Cast<TKey>().ToList();
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary.Keys.ToList();
                }
#endif

				return GenericDictionary.Keys;
			}
		}

		public bool Remove(TKey key)
		{
			if (_dictionary != null)
			{
				if (_dictionary.Contains(key))
				{
					_dictionary.Remove(key);
					return true;
				}
				return false;
			}
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			return GenericDictionary.Remove(key);
		}

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
		public bool TryGetValue(TKey key, out TValue? value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
		{
			if (_dictionary != null)
			{
				if (!_dictionary.Contains(key))
				{
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
					value = default;
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
					return false;
				}
				value = (TValue)_dictionary[key];
				return true;
			}
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			return GenericDictionary.TryGetValue(key, out value);
		}

		public ICollection<TValue> Values
		{
			get
			{
				if (_dictionary != null)
					return _dictionary.Values.Cast<TValue>().ToList();
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary.Values.ToList();
                }
#endif

				return GenericDictionary.Values;
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				if (_dictionary != null)
					return (TValue)_dictionary[key];
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary[key];
                }
#endif

				return GenericDictionary[key];
			}
			set
			{
				if (_dictionary != null)
					_dictionary[key] = value;
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    throw new NotSupportedException();
                }
#endif
				else
					GenericDictionary[key] = value;
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			if (_dictionary != null)
				((IList)_dictionary).Add(item);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			else
				_genericDictionary?.Add(item);
		}

		public void Clear()
		{
			if (_dictionary != null)
				_dictionary.Clear();
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			else
				GenericDictionary.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			if (_dictionary != null)
				return ((IList)_dictionary).Contains(item);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                return _readOnlyDictionary.Contains(item);
            }
#endif

			return GenericDictionary.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (_dictionary != null)
			{
				// Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
				var e = _dictionary.GetEnumerator();
				try
				{
					while (e.MoveNext())
					{
						var entry = e.Entry;
						array[arrayIndex++] = new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value);
					}
				}
				finally
				{
					(e as IDisposable)?.Dispose();
				}
			}
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			else
				GenericDictionary.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				if (_dictionary != null)
					return _dictionary.Count;
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary.Count;
                }
#endif

				return GenericDictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				if (_dictionary != null)
					return _dictionary.IsReadOnly;
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return true;
                }
#endif

				return GenericDictionary.IsReadOnly;
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (_dictionary != null)
			{
				if (_dictionary.Contains(item.Key))
				{
					var value = _dictionary[item.Key];

					if (Equals(value, item.Value))
					{
						_dictionary.Remove(item.Key);
						return true;
					}
					return false;
				}
				return true;
			}
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			return GenericDictionary.Remove(item);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			if (_dictionary != null)
			{
				return _dictionary.Cast<DictionaryEntry>()
					.Select(de => new KeyValuePair<TKey, TValue>((TKey)de.Key, (TValue)de.Value))
					.GetEnumerator();
			}
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                return _readOnlyDictionary.GetEnumerator();
            }
#endif

			return GenericDictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		void IDictionary.Add(object key, object value)
		{
			if (_dictionary != null)
				_dictionary.Add(key, value);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			else
				GenericDictionary.Add((TKey)key, (TValue)value);
		}

		object? IDictionary.this[object key]
		{
			get
			{
				if (_dictionary != null)
					return _dictionary[key];
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary[(TKey)key];
                }
#endif

				return GenericDictionary[(TKey)key];
			}
			set
			{
				if (_dictionary != null)
					_dictionary[key] = value;
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    throw new NotSupportedException();
                }
#endif
				else
				{
					// Consider changing this code to call GenericDictionary.Remove when value is null.
					//
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
					GenericDictionary[(TKey)key] = (TValue)value;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8601 // Possible null reference assignment.
				}
			}
		}

		private readonly struct DictionaryEnumerator<TEnumeratorKey, TEnumeratorValue> : IDictionaryEnumerator
		{
			private readonly IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> _e;

			public DictionaryEnumerator(IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> e)
			{
				ValidationUtils.ArgumentNotNull(e, nameof(e));
				_e = e;
			}

			public DictionaryEntry Entry => (DictionaryEntry)Current;

			public object Key => Entry.Key;

			public object Value => Entry.Value;

			public object Current => new DictionaryEntry(_e.Current.Key, _e.Current.Value);

			public bool MoveNext() => _e.MoveNext();

			public void Reset() => _e.Reset();
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			if (_dictionary != null)
				return _dictionary.GetEnumerator();
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                return new DictionaryEnumerator<TKey, TValue>(_readOnlyDictionary.GetEnumerator());
            }
#endif

			return new DictionaryEnumerator<TKey, TValue>(GenericDictionary.GetEnumerator());
		}

		bool IDictionary.Contains(object key)
		{
			if (_genericDictionary != null)
				return _genericDictionary.ContainsKey((TKey)key);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                return _readOnlyDictionary.ContainsKey((TKey)key);
            }
#endif

			return _dictionary!.Contains(key);
		}

		bool IDictionary.IsFixedSize
		{
			get
			{
				if (_genericDictionary != null)
					return false;
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return true;
                }
#endif

				return _dictionary!.IsFixedSize;
			}
		}

		ICollection IDictionary.Keys
		{
			get
			{
				if (_genericDictionary != null)
					return _genericDictionary.Keys.ToList();
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary.Keys.ToList();
                }
#endif

				return _dictionary!.Keys;
			}
		}

		public void Remove(object key)
		{
			if (_dictionary != null)
				_dictionary.Remove(key);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			else
				GenericDictionary.Remove((TKey)key);
		}

		ICollection IDictionary.Values
		{
			get
			{
				if (_genericDictionary != null)
					return _genericDictionary.Values.ToList();
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary.Values.ToList();
                }
#endif

				return _dictionary!.Values;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (_dictionary != null)
				_dictionary.CopyTo(array, index);
#if HAVE_READ_ONLY_COLLECTIONS
            else if (_readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
#endif
			else
				GenericDictionary.CopyTo((KeyValuePair<TKey, TValue>[])array, index);
		}

		bool ICollection.IsSynchronized
		{
			get
			{
				if (_dictionary != null)
					return _dictionary.IsSynchronized;

				return false;
			}
		}

		object ICollection.SyncRoot
		{
			get
			{
				if (_syncRoot == null) Interlocked.CompareExchange(ref _syncRoot, new object(), null);

				return _syncRoot;
			}
		}

		public object UnderlyingDictionary
		{
			get
			{
				if (_dictionary != null)
					return _dictionary;
#if HAVE_READ_ONLY_COLLECTIONS
                else if (_readOnlyDictionary != null)
                {
                    return _readOnlyDictionary;
                }
#endif

				return GenericDictionary;
			}
		}
	}
}
