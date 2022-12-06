// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <summary>
	/// Utility interface for the generic HTTP request capturing mechanism.
	/// For specific HTTP calls, it can handle the creation of the span and it
	/// can also completely suppress span creation.
	/// </summary>
	internal interface IHttpSpanTracer
	{
		/// <summary>
		/// Determines if a HTTP request is a match for this tracer. This should be
		/// a quick check
		/// </summary>
		/// <param name="method">the HTTP method</param>
		/// <param name="requestUrl">the HTTP request url</param>
		/// <param name="headerGetter">A delegate to retrieve a HTTP header</param>
		/// <returns><c>true</c> if the request is match, <c>false</c> otherwise</returns>
		bool IsMatch(string method, Uri requestUrl, Func<string, string> headerGetter);

		/// <summary>
		/// Starts a new span
		/// </summary>
		/// <param name="agent">The agent used to start the span</param>
		/// <param name="method">the HTTP method</param>
		/// <param name="requestUrl">the HTTP request url</param>
		/// <param name="headerGetter">A delegate to retrieve a HTTP header</param>
		/// <returns>A new instance of a <see cref="ISpan"/>. Can return null</returns>
		ISpan StartSpan(IApmAgent agent, string method, Uri requestUrl, Func<string, string> headerGetter);

		/// <summary>
		/// Determines if a span should be captured for the given HTTP all.
		/// </summary>
		/// <returns>
		/// <c>true</c> if a span should be captured for a given call (either created in the <see cref="StartSpan" /> or
		/// if it fails then by the generic HTTP listener). <c>false</c> if span creation should be suppressed for the given HTTP
		/// call
		/// </returns>
		bool ShouldSuppressSpanCreation();
	}
}
