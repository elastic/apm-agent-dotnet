using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Newtonsoft.Json;

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MetricSampleDto
	{
		public double Value { get; set; }
	}
}
