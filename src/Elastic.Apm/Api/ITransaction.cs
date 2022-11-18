// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Elastic.Apm.Model;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// A transaction describes an event captured by the APM agent instrumentation. They are a special kind of Span that have
	/// additional
	/// attributes associated with them.
	/// </summary>
	/// <remarks>
	/// This interface is the public contract for a transaction. It is not intended to be used by a consumer of the agent to
	/// provide different transaction implementations.
	/// </remarks>
	[Specification("transaction.json")]
	public interface ITransaction : IExecutionSegment
	{
		/// <summary>
		/// Contains data related to FaaS (Function as a Service) events.
		/// </summary>
		public Faas FaaS { get; set; }

		/// <summary>
		/// Any arbitrary contextual information regarding the event, captured by the agent, optionally provided by the user.
		/// This field is lazily initialized, you don't have to assign a value to it and you don't have to null check it either.
		/// </summary>
		Context Context { get; }

		/// <summary>
		/// An arbitrary mapping of additional metadata to store with the event.
		/// Custom is used to add non-indexed, custom contextual information to transactions. Non-indexed means the data is
		/// not searchable or aggregatable in Elasticsearch, and you cannot build dashboards on top of the data. However,
		/// non-indexed information is useful for other reasons, like providing contextual information to help you quickly debug
		/// performance issues or errors.
		/// Unlike <see cref="IExecutionSegment.Labels" /> the data in this property is not trimmed.
		/// </summary>
		Dictionary<string, string> Custom { get; }

		/// <summary>
		/// A snapshot of configuration from when the transaction started. A snapshot contains values
		/// from initial configuration combined with dynamic values from central configuration, if enabled.
		/// </summary>
		public IConfiguration Configuration { get; }

		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		string Result { get; set; }

		/// <summary>
		/// The total number of correlated spans, including started and dropped
		/// </summary>
		[Required]
		SpanCount SpanCount { get; }

		/// <summary>
		/// The type of the transaction.
		/// Example: 'request'
		/// </summary>
		[Required]
		string Type { get; set; }

		/// <summary>
		/// If the transaction does not have a ParentId yet, calling this method generates a new ID, sets it as the ParentId of
		/// this transaction,
		/// and returns it as a <see cref="string" />. If there is already a ParentId, the method just returns the existing
		/// ParentId.
		/// This enables the correlation of the spans the JavaScript Real User Monitoring (RUM) agent
		/// creates for the initial page load with the transaction of the backend service.
		/// </summary>
		/// <returns>
		/// The generated <see cref="IExecutionSegment.ParentId" /> that was applied to the current transaction, or the
		/// existing one.
		/// </returns>
		string EnsureParentId();

		/// <summary>
		/// With this method you can overwrite the service name and version on a per transaction basis.
		/// If this is not set, the transaction will be associated with the default service.
		/// </summary>
		/// <param name="serviceName">The name of the service which the transaction will be associated with.</param>
		/// <param name="serviceVersion">The version of the service which the transaction will be associated with.</param>
		void SetService(string serviceName, string serviceVersion);
	}
}
