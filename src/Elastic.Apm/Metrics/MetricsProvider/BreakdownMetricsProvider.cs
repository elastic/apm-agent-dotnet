// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class BreakdownMetricsProvider : IMetricsProvider
	{
		public int ConsecutiveNumberOfFailedReads { get; set; }

		public string DbgName => nameof(BreakdownMetricsProvider);

		public bool IsMetricAlreadyCaptured => ItemsToSend.Count > 0;

		//TODO make it concurrent
		public List<MetricSet> ItemsToSend { get; set; } = new List<MetricSet>();

		public IEnumerable<MetricSet> GetSamples()
		{
			var retVal = new List<MetricSet>(ItemsToSend);

			ItemsToSend.Clear();

			return retVal;
		}
	}
}
