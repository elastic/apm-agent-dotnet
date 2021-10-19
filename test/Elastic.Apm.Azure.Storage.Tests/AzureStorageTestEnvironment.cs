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

namespace Elastic.Apm.Azure.Storage.Tests
{
	[CollectionDefinition("AzureStorage")]
	public class AzureStorageTestEnvironmentCollection : ICollectionFixture<AzureStorageTestEnvironment>
	{

	}

	/// <summary>
	/// A test environment for Azure Storage that deploys and configures an Azure Storage account
	/// in a given region and location
	/// </summary>
	/// <remarks>
	/// Resource name rules
	/// https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
	/// </remarks>
	public class AzureStorageTestEnvironment : IDisposable
	{
		private readonly TerraformResources _terraform;
		private readonly Dictionary<string, string> _variables;

		public AzureStorageTestEnvironment(IMessageSink messageSink)
		{
			var solutionRoot = SolutionPaths.Root;
			var terraformResourceDirectory = Path.Combine(solutionRoot, "build", "terraform", "azure", "storage");
			var credentials = AzureCredentials.Instance;

			// don't try to run terraform if not authenticated.
			if (credentials is Unauthenticated)
				return;

			_terraform = new TerraformResources(terraformResourceDirectory, credentials, messageSink);

			var resourceGroupName = AzureResources.CreateResourceGroupName("storage-test");
			_variables = new Dictionary<string, string>
			{
				["resource_group"] = resourceGroupName,
				["storage_account_name"] = "dotnet" + Guid.NewGuid().ToString("N").Substring(0, 18),
			};

			_terraform.Init();
			_terraform.Apply(_variables);

			StorageAccountConnectionString = _terraform.Output("connection_string");
			StorageAccountConnectionStringProperties = ParseConnectionString(StorageAccountConnectionString);
		}

		public StorageAccountProperties StorageAccountConnectionStringProperties { get; }

		private static StorageAccountProperties ParseConnectionString(string connectionString)
		{
			var parts = connectionString.Split(';');
			string accountName = null;
			string endpointSuffix = null;
			string defaultEndpointsProtocol = null;

			foreach (var item in parts)
			{
				var kv = item.Split('=');
				switch (kv[0])
				{
					case "AccountName":
						accountName = kv[1];
						break;
					case "EndpointSuffix":
						endpointSuffix = kv[1];
						break;
					case "DefaultEndpointsProtocol":
						defaultEndpointsProtocol = kv[1];
						break;
				}
			}

			return new StorageAccountProperties(defaultEndpointsProtocol, accountName, endpointSuffix);
		}

		public string StorageAccountConnectionString { get; }


		public void Dispose()
		{
			try
			{
				_terraform?.Destroy(_variables);
			}
			catch
			{
				// ignore if there's a problem destroying. The exception will be logged, and in CI, we'll let the cleanup tasks handle
			}
		}
	}

	public class StorageAccountProperties
	{
		public StorageAccountProperties(string defaultEndpointsProtocol, string accountName, string endpointSuffix)
		{
			DefaultEndpointsProtocol = defaultEndpointsProtocol;
			AccountName = accountName;
			EndpointSuffix = endpointSuffix;
		}

		public string AccountName { get; }

		public string EndpointSuffix { get; }

		public string DefaultEndpointsProtocol { get; }

		public string QueueFullyQualifiedNamespace => $"{AccountName}.queue.{EndpointSuffix}";

		public string BlobFullyQualifiedNamespace => $"{AccountName}.blob.{EndpointSuffix}";

		public string FileFullyQualifiedNamespace => $"{AccountName}.file.{EndpointSuffix}";
	}
}
