---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/packages.html
---

# NuGet packages [packages]

Agent instrumentations are released as a set of NuGet packages available on [nuget.org](https://nuget.org). You can add the Agent and specific instrumentations to your .NET application by referencing one or more of these packages.


## Get started [_get_started_2]

* [Azure Cosmos DB](/reference/setup-azure-cosmosdb.md)
* [Azure Service Bus](/reference/setup-azure-servicebus.md)
* [Azure Storage](/reference/setup-azure-storage.md)
* [Entity Framework 6](/reference/setup-ef6.md)
* [Entity Framework Core](/reference/setup-ef-core.md)
* [Elasticsearch](/reference/setup-elasticsearch.md)
* [gRPC](/reference/setup-grpc.md)
* [MongoDB](/reference/setup-mongo-db.md)
* [SqlClient](/reference/setup-sqlclient.md)
* [StackExchange.Redis](/reference/setup-stackexchange-redis.md)


## Packages [_packages]

The following NuGet packages are available:

[**Elastic.Apm**](https://www.nuget.org/packages/Elastic.Apm)
:   The core agent package, containing the [*Public API*](/reference/public-api.md) of the agent. It also contains every tracing component to trace classes that are part of .NET Standard 2.0, which includes the monitoring part for `HttpClient`. Every other Elastic APM package references this package.

[**Elastic.Apm.NetCoreAll**](https://www.nuget.org/packages/Elastic.Apm.NetCoreAll)
:   A meta package that references all other Elastic APM .NET agent package that can automatically configure instrumentation.

    If you plan to monitor a typical ASP.NET Core application that depends on the [Microsoft.AspNetCore.All](https://www.nuget.org/packages/Microsoft.AspNetCore.All) package, you should reference this package.

    In order to avoid adding unnecessary dependencies in applications that aren’t dependent on the [Microsoft.AspNetCore.All](https://www.nuget.org/packages/Microsoft.AspNetCore.All) package, we also offer some other packages - those are all referenced by the `Elastic.Apm.NetCoreAll` package.


[**Elastic.Apm.Extensions.Hosting**](https://www.nuget.org/packages/Elastic.Apm.Extensions.Hosting) ([1.6.0-beta])
:   A package for agent registration integration with `Microsoft.Extensions.Hosting.IHostBuilder` registration.

$$$setup-asp-net$$$

[Elastic.Apm.AspNetCore](/reference/setup-asp-net-core.md)
:   A package for instrumenting ASP.NET Core applications. The main difference between this package and the `Elastic.Apm.NetCoreAll` package is that this package only instruments ASP.NET Core by default, whereas `Elastic.Apm.NetCoreAll` instruments all components that can be automatically configured, such as Entity Framework Core, HTTP calls with `HttpClient`, database calls to SQL Server with `SqlClient`, etc. Additional instrumentations can be added when using `Elastic.Apm.AspNetCore` by referencing the respective NuGet packages and including their configuration code in agent setup.

[**Elastic.Apm.AspNetFullFramework**](/reference/setup-asp-dot-net.md)
:   A package containing ASP.NET .NET Framework instrumentation.

[**Elastic.Apm.Azure.CosmosDb**](/reference/setup-azure-cosmosdb.md)
:   A package containing instrumentation to capture spans for Azure Cosmos DB with [Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Microsoft.Azure.Cosmos), [Microsoft.Azure.DocumentDb](https://www.nuget.org/packages/Microsoft.Azure.DocumentDb), and [Microsoft.Azure.DocumentDb.Core](https://www.nuget.org/packages/Microsoft.Azure.DocumentDb.Core) packages.

[**Elastic.Apm.Azure.ServiceBus**](/reference/setup-azure-servicebus.md)
:   A package containing instrumentation to capture transactions and spans for messages sent and received from Azure Service Bus with [Microsoft.Azure.ServiceBus](https://www.nuget.org/packages/Microsoft.Azure.ServiceBus/) and [Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Azure.Messaging.ServiceBus/) packages.

[**Elastic.Apm.Azure.Storage**](/reference/setup-azure-storage.md)
:   A package containing instrumentation to capture spans for interaction with Azure Storage with [Azure.Storage.Queues](https://www.nuget.org/packages/azure.storage.queues/), [Azure.Storage.Blobs](https://www.nuget.org/packages/azure.storage.blobs/) and [Azure.Storage.Files.Shares](https://www.nuget.org/packages/azure.storage.files.shares/) packages.

[**Elastic.Apm.EntityFrameworkCore**](/reference/setup-ef-core.md)
:   A package containing Entity Framework Core instrumentation.

[**Elastic.Apm.EntityFramework6**](/reference/setup-ef6.md)
:   A package containing an interceptor to automatically create spans for database operations executed by Entity Framework 6 on behalf of the application.

[**Elastic.Apm.MongoDb**](/reference/setup-mongo-db.md)
:   A package containing support for [MongoDB.Driver](https://www.nuget.org/packages/MongoDB.Driver/).

[**Elastic.Apm.SqlClient**](/reference/setup-sqlclient.md)
:   A package containing [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) and [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) instrumentation.

::::{note}
this functionality now included by default in `Elastic.Apm` as of 1.24.0
::::


[**Elastic.Apm.StackExchange.Redis**](/reference/setup-stackexchange-redis.md)
:   A package containing instrumentation to capture spans for commands sent to redis with [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis/) package.
