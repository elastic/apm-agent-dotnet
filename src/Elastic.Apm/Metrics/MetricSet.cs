// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricSetConverter))]
	internal class MetricSet : IMetricSet
	{
		public MetricSet(long timestamp, IEnumerable<MetricSample> samples)
			=> (Timestamp, Samples) = (timestamp, samples);

		/// <inheritdoc />
		public IEnumerable<MetricSample> Samples { get; set; }

		/// <inheritdoc />
		public long Timestamp { get; set; }

		public TransactionInfo Transaction { get; set; }

		public SpanInfo Span { get; set; }
	}

	internal class TransactionInfo
	{
		[MaxLength]
		public string Name { get; set; }
		[MaxLength]
		public string Type { get; set; }
	}

	internal class SpanInfo
	{
		[MaxLength]
		public string Type { get; set; }
		[MaxLength]
		public string SubType { get; set; }
	}
}
