// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Azure.CosmosDb;
using Elastic.Apm.Azure.ServiceBus;
using Elastic.Apm.Azure.Storage;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.MongoDb;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.NetCoreAll
{
	public static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// Adds the Elastic APM Middleware to the ASP.NET Core pipeline and enables
		/// <see cref="HttpDiagnosticsSubscriber" />,
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
		/// This method turns on ASP.NET Core monitoring with every other related monitoring components, for example the agent
		/// will also automatically trace outgoing HTTP requests and database statements.
		/// </summary>
		/// <returns>The application builder instance</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the <see cref="IConfiguration"/> of your application to the Elastic APM Agent.
		/// The agent reads agent-related configuration from the <see cref="IConfiguration"/> instance, and uses it to configure the agent.
		/// If no <see cref="IConfiguration" /> is provided, the agent reads agent-related configuration from environment variables.
		/// </param>
		[Obsolete("This extension is maintained for backward compatibility." +
			" We recommend registering the agent via the IServiceCollection using the AddAllElasticApm extension method instead. This method may be removed in a future release.")]
		public static IApplicationBuilder UseAllElasticApm(
			this IApplicationBuilder builder,
			IConfiguration configuration = null
		) => AspNetCore.ApplicationBuilderExtensions
			.UseElasticApm(builder, configuration,
				new HttpDiagnosticsSubscriber(),
				new SqlClientDiagnosticSubscriber(),
				new EfCoreDiagnosticsSubscriber(),
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
