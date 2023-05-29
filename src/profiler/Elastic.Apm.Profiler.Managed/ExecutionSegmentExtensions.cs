// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;

namespace Elastic.Apm.Profiler.Managed
{
	internal static class ExecutionSegmentExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndCapturingException(this IExecutionSegment segment, Exception exception)
		{
			if (segment is not null)
			{
				if (exception is not null)
					segment.CaptureException(exception);

				segment.End();
			}
		}
	}
}
