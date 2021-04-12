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

#nullable enable
namespace Elastic.Apm.Libraries.Newtonsoft.Json
{
	/// <summary>
	/// The default JSON name table implementation.
	/// </summary>
	internal class DefaultJsonNameTable : JsonNameTable
	{
		// used to defeat hashtable DoS attack where someone passes in lots of strings that hash to the same hash code
		private static readonly int HashCodeRandomizer;

		static DefaultJsonNameTable() => HashCodeRandomizer = Environment.TickCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultJsonNameTable" /> class.
		/// </summary>
		public DefaultJsonNameTable() => _entries = new Entry[_mask + 1];

		private int _count;
		private Entry[] _entries;
		private int _mask = 31;

		/// <summary>
		/// Gets a string containing the same characters as the specified range of characters in the given array.
		/// </summary>
		/// <param name="key">The character array containing the name to find.</param>
		/// <param name="start">The zero-based index into the array specifying the first character of the name.</param>
		/// <param name="length">The number of characters in the name.</param>
		/// <returns>A string containing the same characters as the specified range of characters in the given array.</returns>
		public override string? Get(char[] key, int start, int length)
		{
			if (length == 0) return string.Empty;

			var hashCode = length + HashCodeRandomizer;
			hashCode += (hashCode << 7) ^ key[start];
			var end = start + length;
			for (var i = start + 1; i < end; i++) hashCode += (hashCode << 7) ^ key[i];
			hashCode -= hashCode >> 17;
			hashCode -= hashCode >> 11;
			hashCode -= hashCode >> 5;

			// make sure index is evaluated before accessing _entries, otherwise potential race condition causing IndexOutOfRangeException
			var index = hashCode & _mask;
			var entries = _entries;

			for (var entry = entries[index]; entry != null; entry = entry.Next)
			{
				if (entry.HashCode == hashCode && TextEquals(entry.Value, key, start, length)) return entry.Value;
			}

			return null;
		}

		/// <summary>
		/// Adds the specified string into name table.
		/// </summary>
		/// <param name="key">The string to add.</param>
		/// <remarks>This method is not thread-safe.</remarks>
		/// <returns>The resolved string.</returns>
		public string Add(string key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			var length = key.Length;
			if (length == 0) return string.Empty;

			var hashCode = length + HashCodeRandomizer;
			for (var i = 0; i < key.Length; i++) hashCode += (hashCode << 7) ^ key[i];
			hashCode -= hashCode >> 17;
			hashCode -= hashCode >> 11;
			hashCode -= hashCode >> 5;
			for (var entry = _entries[hashCode & _mask]; entry != null; entry = entry.Next)
			{
				if (entry.HashCode == hashCode && entry.Value.Equals(key, StringComparison.Ordinal)) return entry.Value;
			}

			return AddEntry(key, hashCode);
		}

		private string AddEntry(string str, int hashCode)
		{
			var index = hashCode & _mask;
			var entry = new Entry(str, hashCode, _entries[index]);
			_entries[index] = entry;
			if (_count++ == _mask) Grow();
			return entry.Value;
		}

		private void Grow()
		{
			var entries = _entries;
			var newMask = _mask * 2 + 1;
			var newEntries = new Entry[newMask + 1];

			for (var i = 0; i < entries.Length; i++)
			{
				Entry next;
				for (var entry = entries[i]; entry != null; entry = next)
				{
					var index = entry.HashCode & newMask;
					next = entry.Next;
					entry.Next = newEntries[index];
					newEntries[index] = entry;
				}
			}
			_entries = newEntries;
			_mask = newMask;
		}

		private static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length)
		{
			if (str1.Length != str2Length) return false;

			for (var i = 0; i < str1.Length; i++)
			{
				if (str1[i] != str2[str2Start + i]) return false;
			}
			return true;
		}

		private class Entry
		{
			internal readonly int HashCode;
			internal readonly string Value;

			internal Entry(string value, int hashCode, Entry next)
			{
				Value = value;
				HashCode = hashCode;
				Next = next;
			}

			internal Entry Next;
		}
	}
}
