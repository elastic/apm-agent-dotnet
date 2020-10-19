// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// An event captured by an agent occurring in a monitored service
	/// </summary>
	[Specification("docs/spec/spans/span.json")]
	public interface ISpan : IExecutionSegment
	{
		/// <summary>
		/// The action of the span.
		/// Examples: 'query'.
		/// </summary>
		string Action { get; set; }

		/// <summary>
		/// Any other arbitrary data captured by the agent, optionally provided by the user.
		/// </summary>
		SpanContext Context { get; }

		/// <summary>
		/// The stack trace which was captured for the given span.
		/// </summary>
		List<CapturedStackFrame> StackTrace { get; }

		/// <summary>
		/// The subtype of the span.
		/// Examples: 'http', 'mssql'.
		/// </summary>
		string Subtype { get; set; }

		/// <summary>
		/// The timestamp of the span
		/// </summary>
		long Timestamp { get; }

		/// <summary>
		/// Hex encoded 64 random bits ID of the correlated transaction.
		/// </summary>
		string TransactionId { get; }

		/// <summary>
		/// The type of the span.
		/// Examples: 'db', 'external'.
		/// </summary>
		string Type { get; set; }
	}
}
