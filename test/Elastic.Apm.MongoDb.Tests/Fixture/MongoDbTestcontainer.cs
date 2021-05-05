// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers.Containers.Configurations;
using DotNet.Testcontainers.Containers.Modules.Abstractions;

namespace Elastic.Apm.MongoDb.Tests.Fixture
{
	public sealed class MongoDbTestcontainer : TestcontainerDatabase
	{
		internal MongoDbTestcontainer(ITestcontainersConfiguration configuration)
			: base(configuration)
		{
		}

		public override string ConnectionString =>
			string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)
				? $@"mongodb://{Hostname}:{Port}"
				: $@"mongodb://{Username}:{Password}@{Hostname}:{Port}";
	}
}
