using System.Collections.Generic;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MetricSetDto
	{
		public Dictionary<string, MetricSample> Samples { get; set; }

		[JsonProperty("timestamp")]
		public long TimeStamp { get; set; }
	}
}
