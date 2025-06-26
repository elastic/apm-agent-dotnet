// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates distributed tracing data passed from the caller to the callee in order to correlate calls between them.
	/// Its purpose is similar to that of traceparent and tracestate headers described in the
	/// <a href="https://www.w3.org/TR/trace-context/">Trace Context specification</a>
	/// </summary>
	/// <example>
	/// See sample/ApiSamples/Program.cs for an example on how to manually pass distributed tracing data between the caller
	/// and the callee
	/// </example>
	public class DistributedTracingData
	{
		internal DistributedTracingData(string traceId, string parentId, bool flagRecorded, TraceState traceState = null)
		{
			TraceId = traceId;
			ParentId = parentId;
			FlagRecorded = flagRecorded;
			TraceState = traceState;
		}

		internal bool FlagRecorded { get; }
		internal bool HasTraceState => TraceState != null;
		internal string ParentId { get; }
		internal string TraceId { get; }
		internal TraceState TraceState { get; }

		/// <summary>
		/// Serializes this instance to a traceparent string.
		/// This method should be used at the caller side in pairs with <see cref="SerializeTraceStateToString" />,
		/// and the return value should be passed to the (possibly remote) callee side.
		/// <see cref="Create" /> should be used to deserialize the instance at the callee side.
		/// </summary>
		/// <returns>
		/// String containing the traceparent data in serialized form.
		/// </returns>
		public string SerializeToString() => TraceContext.BuildTraceparent(this);

		/// <summary>
		/// Serializes this instance to a tracestate string.
		/// This method should be used at the caller side in pairs with <see cref="SerializeToString" />,
		/// and the return value should be passed to the (possibly remote) callee side.
		/// <see cref="Create" /> should be used to deserialize the instance at the callee side.
		/// </summary>
		/// <returns>The string representation of the tracestate header, or null if there is no tracestate.</returns>
		public string SerializeTraceStateToString() => TraceState?.ToTextHeader();

		/// <summary>
		/// Deserializes an instance from a string.
		/// This method should be used at the callee side and the deserialized instance can be passed to
		/// <see cref="ITracer.StartTransaction" />.
		/// </summary>
		/// <param name="serialized">should be a return value from a call to <see cref="SerializeToString" />.</param>
		/// <returns>
		/// Instance deserialized from <paramref name="serialized" />.
		/// </returns>
		public static DistributedTracingData TryDeserializeFromString(string serialized) => TraceContext.TryExtractTracingData(serialized);

		/// <summary>
		/// Creates an instance from a treceparent and tracestate strings.
		/// This method should be used at the callee side, and the created instance can be passed to
		/// <see cref="ITracer.StartTransaction" />.
		/// </summary>
		/// <param name="traceParent">should be a return value from a call to <see cref="SerializeToString" />.</param>
		/// <param name="traceState">should be a return value from a call to <see cref="SerializeTraceStateToString"/>.</param>
		/// <returns>
		/// Instance deserialized from <paramref name="traceParent" /> and <paramref name="traceState" /> .
		/// </returns>
		public static DistributedTracingData Create(string traceParent, string traceState) =>
			TraceContext.TryExtractTracingData(traceParent, traceState);

		public override string ToString() => new ToStringBuilder(nameof(DistributedTracingData))
		{
			{ nameof(TraceId), TraceId },
			{ nameof(ParentId), ParentId },
			{ nameof(FlagRecorded), FlagRecorded },
			{ nameof(TraceState), TraceState?.ToTextHeader() }
		}.ToString();
	}
}
