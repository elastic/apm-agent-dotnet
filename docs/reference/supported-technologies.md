---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/supported-technologies.html
---

# Supported technologies [supported-technologies]

If your favorite technology is not supported yet, you can vote for it by participating in our [survey](https://docs.google.com/forms/d/18SgsVo9asGNFMjRqwdrk3wTHNwPhtHv4jE35hZRCL6A/). We will use the results to add support for the most requested technologies.

Another option is to add a dependency to the agent’s [public API](/reference/public-api.md) in order to programmatically create custom transactions and spans.

If you want to extend the auto-instrumentation capabilities of the agent, the [contributing guide](https://github.com/elastic/apm-agent-dotnet/blob/main/CONTRIBUTING.md) should get you started.

::::{note}
If, for example, the HTTP client library of your choice is not listed, it means that there won’t be spans for those outgoing HTTP requests. If the web framework you are using is not supported, the agent will not capture transactions.
::::



## .NET versions [supported-dotnet-flavors]

The agent works on every .NET flavor and version that supports .NET Standard 2.0. This means .NET Core 2.0 or newer, and .NET Framework 4.6.2* or newer.

** Due to binding issues introduced by Microsoft, we recommend at least .NET Framework 4.7.2 for best compatibility.*

::::{important}
While this library **should** work on .NET Core 2.0+, we limit our support to only those versions currently supported by Microsoft - .NET 6.0 and newer.

::::



## Web frameworks [supported-web-frameworks]

Automatic instrumentation for a web framework means a transaction is automatically created for each incoming request and it is named after the registered route.

Automatic instrumentation is supported for the following web frameworks

| Framework | Supported versions | Integration |
| --- | --- | --- |
| ASP.NET Core [1.0] | 2.1+ | [NuGet package](/reference/setup-asp-net-core.md) |
| ASP.NET (.NET Framework) in IIS  [1.1] | 4.6.2+ (IIS 7.0 or newer) | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md)<br>*or*<br>[NuGet package](/reference/setup-asp-dot-net.md) |


## RPC Frameworks [supported-rpc-frameworks]

The agent supports gRPC on .NET Core both on the client and the server side. Every gRPC call is automatically captured by the agent.

Streaming is not supported; for streaming use-cases, the agent does not create transactions and spans automatically.

| Framework | Supported versions | Integration |
| --- | --- | --- |
| gRPC [1.7] | Grpc.Net.Client 2.23.2+ *(client side)* | [NuGet package](/reference/setup-grpc.md) |
| ASP.NET Core 2.1+ *(server side)* | [NuGet package](/reference/setup-asp-net-core.md) |


## Data access technologies [supported-data-access-technologies]

Automatic instrumentation is supported for the following data access technologies

| Data access technology | Supported versions | Integration |
| --- | --- | --- |
| Azure CosmosDB [1.11] | Microsoft.Azure.Cosmos 3.0.0+ | [NuGet package](/reference/setup-azure-cosmosdb.md) |
| Microsoft.Azure.DocumentDB.Core 2.4.1+ |
| Microsoft.Azure.DocumentDB 2.4.1+ |
| Entity Framework Core [1.0] | Microsoft.EntityFrameworkCore 2.x+ | [NuGet package](/reference/setup-ef-core.md) |
| Entity Framework 6 [1.2] | EntityFramework 6.2+ | [NuGet package](/reference/setup-ef6.md) |
| Elasticsearch [1.6] | Elasticsearch.Net 7.6.0+ | [NuGet package](/reference/setup-elasticsearch.md) |
| NEST 7.6.0+ |
| MySQL [1.12] | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| MongoDB [1.9] | MongoDB.Driver 2.19.0+ | [NuGet package](/reference/setup-mongo-db.md) |
| Oracle [1.12] | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| PostgreSQL [1.12] | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| Redis [1.8] | StackExchange.Redis 2.0.495+ | [NuGet package](/reference/setup-stackexchange-redis.md) |
| SqlClient | System.Data.SqlClient 2.0.495+ [1.8] | [NuGet package](/reference/setup-sqlclient.md) |
| See profiler documentation [1.12] | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| SQLite [1.12] | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |


## Messaging systems [supported-messaging-systems]

We support automatic instrumentation for the following messaging systems

| Messaging system | Supported versions | Integration |
| --- | --- | --- |
| Azure Service Bus [1.10] | Microsoft.Azure.ServiceBus 3.0.0+ | [NuGet package](/reference/setup-azure-servicebus.md) |
| Azure.Messaging.ServiceBus 7.0.0+ |
| Azure Queue Storage [1.10] | Azure.Storage.Queues 12.6.0+ | [NuGet package](/reference/setup-azure-storage.md) |
| Kafka [1.12] | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| RabbitMQ [1.12] | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |


## Networking client-side technologies [supported-networking-client-side-technologies]

Automatic instrumentation for networking client-side technology means an HTTP span is automatically created for each outgoing HTTP request and tracing headers are propagated.

| Framework | Supported versions | Integration |
| --- | --- | --- |
| System.Net.Http.HttpClient [1.0] | *built-in* | [part of Elastic.Apm](/reference/public-api.md#setup-http) |
| System.Net.HttpWebRequest [1.1] |


## Cloud services [supported-cloud-services]

Automatic instrumentation for the following cloud services

| Cloud service | Supported versions | Integration |
| --- | --- | --- |
| Azure CosmosDB [1.11] | Microsoft.Azure.Cosmos 3.0.0+ | [NuGet package](/reference/setup-azure-cosmosdb.md) |
| Microsoft.Azure.DocumentDB.Core 2.4.1+ |
| Microsoft.Azure.DocumentDB 2.4.1+ |
| Azure Service Bus [1.10] | Microsoft.Azure.ServiceBus 3.0.0+ | [NuGet package](/reference/setup-azure-servicebus.md) |
| Azure.Messaging.ServiceBus 7.0.0+ |
| Azure Storage [1.10] | Azure.Storage.Blobs 12.8.0+ | [NuGet package](/reference/setup-azure-storage.md) |
| Azure.Storage.Queues 12.6.0+ |
| Azure.Storage.Files.Shares 12.6.0+ |

