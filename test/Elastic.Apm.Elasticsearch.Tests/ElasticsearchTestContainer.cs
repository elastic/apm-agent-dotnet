// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


using System.Linq;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public sealed class ElasticsearchTestContainer : TestcontainerDatabase
	{
		internal ElasticsearchTestContainer(ITestcontainersConfiguration configuration, ILogger logger)
			: base(configuration, logger) => ContainerPort = int.Parse(configuration.ExposedPorts.First().Value);

		public override string ConnectionString => $"http://{Hostname}:{Port}";
	}
}
