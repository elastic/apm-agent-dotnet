// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;

namespace Elastic.Apm.Profiler.Managed
{
	internal static class TracerExtensions
	{
		public static IExecutionSegment CurrentExecutionSegment(this ITracer tracer) => (IExecutionSegment)tracer.CurrentSpan ?? tracer.CurrentTransaction;
	}
}
