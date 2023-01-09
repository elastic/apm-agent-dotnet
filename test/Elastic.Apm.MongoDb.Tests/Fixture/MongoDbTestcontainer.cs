// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Elastic.Apm.MongoDb.Tests.Fixture
{
	public sealed class MongoDbTestcontainer : TestcontainerDatabase
	{
		internal MongoDbTestcontainer(ITestcontainersConfiguration configuration, ILogger logger)
			: base(configuration, logger)
		{
		}

		public override string ConnectionString =>
			string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)
				? $@"mongodb://{Hostname}:{Port}"
				: $@"mongodb://{Username}:{Password}@{Hostname}:{Port}";
	}
}
