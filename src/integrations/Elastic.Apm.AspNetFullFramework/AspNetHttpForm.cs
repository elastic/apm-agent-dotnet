// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class AspNetHttpForm : IHttpFormAdapter
	{
		private readonly NameValueCollection _form;

		public AspNetHttpForm(NameValueCollection form) => _form = form;

		public int Count => _form?.Count ?? 0;

		public bool HasValue => _form != null;

		public IEnumerator<(string Key, string Value)> GetEnumerator() => new AspNetHttpFormEnumerator(_form);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class AspNetHttpFormEnumerator : IEnumerator<(string Key, string Value)>
	{
		private int _pos = -1;
		private readonly NameValueCollection _form;

		public AspNetHttpFormEnumerator(NameValueCollection form) => _form = form;

		public (string Key, string Value) Current
		{
			get
			{
				var key = _form.AllKeys[_pos];
				return (key, _form[key]);
			}
		}
		object IEnumerator.Current => Current;
		public void Dispose() { }
		public bool MoveNext()
		{
			_pos++;
			return _pos < _form.AllKeys.Length;
		}
		public void Reset() => _pos = -1;
	}
}
