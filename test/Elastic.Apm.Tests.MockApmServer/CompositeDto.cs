// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class CompositeDto
	{
		[JsonProperty("compression_strategy")]
		public string CompressionStrategy { get; set; }

		public int Count { get; set; }

		public double Sum { get; set; }
	}
}
