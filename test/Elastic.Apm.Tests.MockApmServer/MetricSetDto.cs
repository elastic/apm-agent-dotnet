using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MetricSetDto
	{
		public Dictionary<string, MetricSampleDto> Samples { get; set; }

		public long Timestamp { get; set; }
	}
}
