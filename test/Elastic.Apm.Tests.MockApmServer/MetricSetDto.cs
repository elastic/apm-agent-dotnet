// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	[Specification("docs/spec/v2/metricset.json")]
	internal class MetricSetDto : ITimestampedDto
	{
		public Dictionary<string, MetricSampleDto> Samples { get; set; }

		public long Timestamp { get; set; }

		public override string ToString()
		{
			var resultBuilder = new ToStringBuilder(nameof(MetricSetDto));
			resultBuilder.Add("Timestamp", Timestamp);
			var samplesToStringBuilder = new ToStringBuilder("");
			foreach (var sample in Samples) resultBuilder.Add(sample.Key, sample.Value);
			resultBuilder.Add("samples", samplesToStringBuilder.ToString());
			return resultBuilder.ToString();
		}

		public void AssertValid() => MetricsAssertValid.AssertValid(this);
	}
}
