// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Azure.CosmosDb;
using Elastic.Apm.Azure.ServiceBus;
using Elastic.Apm.Azure.Storage;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.MongoDb;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers Elastic APM .NET Agent into the dependency injection container and enables:
	/// <list type="bullet">
	///   <item>
	///    <see cref="HttpDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="AspNetCoreDiagnosticSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="EfCoreDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="SqlClientDiagnosticSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="ElasticsearchDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="GrpcClientDiagnosticSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="AzureMessagingServiceBusDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="MicrosoftAzureServiceBusDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="AzureBlobStorageDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="AzureQueueStorageDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="AzureFileShareStorageDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="AzureCosmosDbDiagnosticsSubscriber" />
	///   </item>
	///   <item>
	///    <see cref="MongoDbDiagnosticsSubscriber" />
	///   </item>
	/// </list>
	/// </summary>
	/// <param name="services">An <see cref="IServiceCollection"/> where services are to be registered.</param>
	public static IServiceCollection AddAllElasticApm(this IServiceCollection services) =>
		services.AddElasticApm(
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
