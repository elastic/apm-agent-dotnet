// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
#if NET5_0_OR_GREATER

using System;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;

namespace Elastic.Apm.OpenTelemetry
{
	/// <summary>
	/// Handles HTTP spans for outgoing HTTP calls from `Elastic.Clients.ElasticSearch`.
	/// Since `Elastic.Clients.ElasticSearch` emits <see cref="Activity"/>, and according to our spec, for calls into elasticsearch
	/// we don't need to create an extra HTTP span, all this does is that it suppresses span creation during the outgoing HTTP call.
	/// </summary>
	public class ElasticSearchHttpNonTracer : IHttpSpanTracer
	{
		public bool IsMatch(string method, Uri requestUrl, Func<string, string> headerGetter) => false;
		public ISpan StartSpan(IApmAgent agent, string method, Uri requestUrl, Func<string, string> headerGetter) => null;

		public bool ShouldSuppressSpanCreation()
		{
			if (Activity.Current == null || Activity.Current.Parent == null)
				return false;
			return Activity.Current.Parent.DisplayName.StartsWith("Elasticsearch:") && Activity.Current.Parent.Tags.Any(n => n is { Key: "db.system", Value: "elasticsearch" });
		}
	}
}

#endif
