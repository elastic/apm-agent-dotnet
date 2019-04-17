using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
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

		public string Serialize() => TraceParent.BuildTraceparent(this);

		public static DistributedTracingData TryDeserialize(string serialized) => TraceParent.TryExtractTraceparent(serialized);

		public override string ToString() => new ToStringBuilder(nameof(DistributedTracingData))
		{
			{ "TraceId", TraceId },
			{ "ParentId", ParentId },
			{ "FlagRecorded", FlagRecorded }
		}.ToString();
	}
}
