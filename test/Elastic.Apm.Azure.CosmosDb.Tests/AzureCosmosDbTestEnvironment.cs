// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.Terraform;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.CosmosDb.Tests
{
	[CollectionDefinition("AzureCosmosDb")]
	public class AzureCosmosDbTestEnvironmentCollection : ICollectionFixture<AzureCosmosDbTestEnvironment>
	{

	}

	/// <summary>
	/// A test environment for Azure CosmosDb that deploys and configures an Azure CosmosDb account
	/// in a given region and location
	/// </summary>
	/// <remarks>
	/// Resource name rules
	/// https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
	/// </remarks>
	public class AzureCosmosDbTestEnvironment : IDisposable
	{
		private readonly TerraformResources _terraform;
		private readonly Dictionary<string, string> _variables;

		public AzureCosmosDbTestEnvironment(IMessageSink messageSink)
		{
			var solutionRoot = SolutionPaths.Root;
			var terraformResourceDirectory = Path.Combine(solutionRoot, "build", "terraform", "azure", "cosmosdb");
			var credentials = AzureCredentials.Instance;

			// don't try to run terraform if not authenticated.
			if (credentials is Unauthenticated)
				return;

			_terraform = new TerraformResources(terraformResourceDirectory, credentials, messageSink);

			var machineName = Environment.MachineName.ToLowerInvariant();
			if (machineName.Length > 65)
				machineName = machineName.Substring(0, 65);

			_variables = new Dictionary<string, string>
			{
				["resource_group"] = $"dotnet-{machineName}-cosmosdb-test",
				["cosmos_db_account_name"] = "dotnet" + Guid.NewGuid().ToString("N").Substring(0, 18),
			};

			_terraform.Init();
			_terraform.Apply(_variables);
			Endpoint = _terraform.Output("endpoint");
			PrimaryMasterKey = _terraform.Output("primary_master_key");
		}

		public string Endpoint { get; }

		public string PrimaryMasterKey { get; }

		public void Dispose() => _terraform?.Destroy(_variables);
	}
}
