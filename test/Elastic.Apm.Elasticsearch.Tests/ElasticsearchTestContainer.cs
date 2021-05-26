// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers.Containers.Configurations;
using DotNet.Testcontainers.Containers.Modules.Abstractions;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public class ElasticsearchTestContainer : HostedServiceContainer
	{
		internal ElasticsearchTestContainer(TestcontainersConfiguration configuration) : base(configuration) => Hostname = "localhost";

		public override string ConnectionString => $"http://{Hostname}:{Port}";
	}
}
