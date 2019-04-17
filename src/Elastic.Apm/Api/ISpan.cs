using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elastic.Apm.Api
{
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

		/// <summary>
		/// Distributed tracing data for this segment as the distributed tracing caller.
		/// </summary>
		DistributedTracingData OutgoingDistributedTracingData { get; }
	}
}
