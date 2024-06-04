// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Azure.CosmosDb;
using Elastic.Apm.Azure.ServiceBus;
using Elastic.Apm.Azure.Storage;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.MongoDb;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.NetCoreAll
{
	public static class HostBuilderExtensions
	{
		/// <summary>
		/// Register Elastic APM .NET Agent with components in the container and enables
		/// <see cref="HttpDiagnosticsSubscriber" />,
		/// <see cref="AspNetCoreDiagnosticSubscriber" />,
		/// <see cref="EfCoreDiagnosticsSubscriber" />,
		/// <see cref="SqlClientDiagnosticSubscriber"/>,
		/// <see cref="ElasticsearchDiagnosticsSubscriber"/>.
		/// <see cref="GrpcClientDiagnosticSubscriber"/>,
		/// <see cref="AzureMessagingServiceBusDiagnosticsSubscriber"/>,
		/// <see cref="MicrosoftAzureServiceBusDiagnosticsSubscriber"/>,
		/// <see cref="AzureBlobStorageDiagnosticsSubscriber"/>,
		/// <see cref="AzureQueueStorageDiagnosticsSubscriber"/>,
		/// <see cref="AzureFileShareStorageDiagnosticsSubscriber"/>,
		/// <see cref="AzureCosmosDbDiagnosticsSubscriber"/>,
		/// and <see cref="MongoDbDiagnosticsSubscriber"/>.
		/// </summary>
		/// <param name="builder">Builder.</param>
		[Obsolete("This extension is maintained for backward compatibility." +
			" We recommend registering the agent via the IServiceCollection using the AddAllElasticApm extension method instead.")]
		public static IHostBuilder UseAllElasticApm(this IHostBuilder builder) => builder.UseElasticApm(
			new HttpDiagnosticsSubscriber(),
			new AspNetCoreDiagnosticSubscriber(),
			new EfCoreDiagnosticsSubscriber(),
			new SqlClientDiagnosticSubscriber(),
			new ElasticsearchDiagnosticsSubscriber(),
			new GrpcClientDiagnosticSubscriber(),
			new AzureMessagingServiceBusDiagnosticsSubscriber(),
			new MicrosoftAzureServiceBusDiagnosticsSubscriber(),
			new AzureBlobStorageDiagnosticsSubscriber(),
			new AzureQueueStorageDiagnosticsSubscriber(),
			new AzureFileShareStorageDiagnosticsSubscriber(),
			new AzureCosmosDbDiagnosticsSubscriber(),
			new MongoDbDiagnosticsSubscriber());
	}
}
