// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;

namespace Elastic.Apm.DiagnosticListeners
{
	internal interface IHttpSpanEnricher
	{
		bool IsMatch(string method, Uri requestUrl);

		void Enrich(string method, Uri requestUrl, Func<string, string[]> headerGetter, ISpan span);
	}
}
