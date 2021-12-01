// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Tests.Utilities
{
	internal class MockApmServerInfo : IApmServerInfo
	{
		public static MockApmServerInfo Version710 { get; } = new MockApmServerInfo(new ElasticVersion(7, 10, 0, null));

		public static MockApmServerInfo Version716 { get; } = new MockApmServerInfo(new ElasticVersion(7, 16, 0, string.Empty));

		public MockApmServerInfo(ElasticVersion version) => Version = version;

		public ElasticVersion Version { get; set; }
	}
}
