// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Metrics;

namespace Elastic.Apm.Tests.Mocks
{
	public class FakeMetricsCollector : IMetricsCollector
	{
		public void StartCollecting() { }
	}
}
