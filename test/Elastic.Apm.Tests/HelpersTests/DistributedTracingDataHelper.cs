using Elastic.Apm.Api;

namespace Elastic.Apm.Tests.HelpersTests
{
	internal static class DistributedTracingDataHelper
	{
		internal const string ValidParentId = "5ec5de4fdae36f4c";
		internal const string ValidTraceFlags = "01";
		internal const string ValidTraceId = "005a6663c2fb9591a0e53d322df6c3e2";

		internal static DistributedTracingData BuildDistributedTracingData(string traceId, string parentId, string traceFlags) =>
			DistributedTracingData.TryDeserializeFromString(
				"00-" + // version
				(traceId == null ? "" : $"{traceId}") +
				(parentId == null ? "" : $"-{parentId}") +
				(traceFlags == null ? "" : $"-{traceFlags}"));
	}
}
