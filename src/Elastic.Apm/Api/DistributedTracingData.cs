using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// An object encapsulating data passed from the caller to the callee in distributed tracing in order to correlate between
	/// them.
	/// Its purpose is similar to that of "traceparent" header described at https://www.w3.org/TR/trace-context/
	/// See samples/ApiSamples/Program.cs for an example on how to manually pass distributed tracing data between the caller
	/// and the callee.
	/// </summary>
	public class DistributedTracingData
	{
		internal DistributedTracingData(string traceId, string parentId, bool flagRecorded)
		{
			TraceId = traceId;
			ParentId = parentId;
			FlagRecorded = flagRecorded;
		}

		internal bool FlagRecorded { get; }
		internal string ParentId { get; }

		internal string TraceId { get; }

		/// <summary>
		/// Serializes this instance to a string.
		/// This method should be used at the caller side and the return value should be passed to the (possibly remote) callee
		/// side.
		/// <see cref="TryDeserializeFromString" /> should be used to deserialize the instance at the callee side.
		/// </summary>
		/// <returns>
		/// String containing the instance in serialized form.
		/// </returns>
		public string SerializeToString() => TraceParent.BuildTraceparent(this);

		/// <summary>
		/// Deserializes an instance from a string.
		/// This method should be used at the callee side and the deserialized instance can be passed to
		/// <see cref="ITracer.StartTransaction" />.
		/// </summary>
		/// <param name="serialized">should be a return value from a call to <see cref="SerializeToString" />.</param>
		/// <returns>
		/// Instance deserialized from
		/// <param name="serialized" />
		/// .
		/// </returns>
		public static DistributedTracingData TryDeserializeFromString(string serialized) => TraceParent.TryExtractTraceparent(serialized);

		public override string ToString() => new ToStringBuilder(nameof(DistributedTracingData))
		{
			{ "TraceId", TraceId }, { "ParentId", ParentId }, { "FlagRecorded", FlagRecorded }
		}.ToString();
	}
}
