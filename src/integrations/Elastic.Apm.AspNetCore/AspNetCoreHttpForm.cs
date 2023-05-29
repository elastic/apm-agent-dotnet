// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Elastic.Apm.AspNetCore
{
	internal class AspNetCoreHttpForm : IHttpFormAdapter
	{
		private readonly IFormCollection _form;

		public AspNetCoreHttpForm(IFormCollection form) => _form = form;

		public int Count => _form?.Count ?? 0;

		public bool HasValue => _form != null;

		public IEnumerator<(string Key, string Value)> GetEnumerator() => new AspNetCoreHttpFormEnumerator(_form?.GetEnumerator());
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class AspNetCoreHttpFormEnumerator : IEnumerator<(string Key, string Value)>
	{
		private readonly IEnumerator<KeyValuePair<string, StringValues>> _enumerator;

		public AspNetCoreHttpFormEnumerator(IEnumerator<KeyValuePair<string, StringValues>> enumerator) => _enumerator = enumerator;
		public (string Key, string Value) Current => (_enumerator.Current.Key, _enumerator.Current.Value);
		object IEnumerator.Current => Current;
		public void Dispose() => _enumerator?.Dispose();
		public bool MoveNext() => _enumerator?.MoveNext() ?? false;
		public void Reset() => _enumerator?.Reset();
	}
}
