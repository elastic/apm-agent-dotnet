---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/supported-technologies.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Supported technologies [supported-technologies]


## .NET versions [supported-dotnet-flavors]

The APM Agent for .NET targets every .NET flavor and version that supports .NET Standard 2.0 or .NET Standard 2.1.

However, we only test and support .NET runtimes that are also supported per the [Microsoft .NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core). Therefore, we always recommend you upgrade to a supported runtime before raising issues.

::::{note}
On .NET Framework, due to binding issues introduced by Microsoft, we recommend at least .NET Framework 4.7.2 for best compatibility.
::::


## Web frameworks [supported-web-frameworks]

Automatic instrumentation for a web framework means a transaction is automatically created for each incoming request and it is named after the registered route.

Automatic instrumentation is supported for the following web frameworks

| Framework | Supported versions | Integration |
| --- | --- | --- |
| ASP.NET Core {applies_to}`apm_agent_dotnet: ga 1.0` | 8, 9 | [NuGet package](/reference/setup-asp-net-core.md) |
| ASP.NET (.NET Framework) in IIS  {applies_to}`apm_agent_dotnet: ga 1.1` | 4.6.2-4.8.1 (IIS 10) | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md)<br>*or*<br>[NuGet package](/reference/setup-asp-dot-net.md) |

::::{note}
We support ASP.NET on IIS 10 versions supported by Microsoft per their [IIS support policy](https://learn.microsoft.com/lifecycle/products/internet-information-services-iis).
IIS must be installed on a [supported](https://learn.microsoft.com/windows/release-health/windows-server-release-info#windows-server-major-versions-by-servicing-option--) Windows operating system version.
::::


## RPC Frameworks [supported-rpc-frameworks]

The agent supports gRPC on .NET both on the client and the server side. Every gRPC call is automatically captured by the agent.

Streaming is not supported; for streaming use-cases, the agent does not create transactions and spans automatically.

| Framework | Supported versions | Integration |
| --- | --- | --- |
| gRPC {applies_to}`apm_agent_dotnet: ga 1.7` | Grpc.Net.Client 2.23.2-2.71.0 *(client side)* | [NuGet package](/reference/setup-grpc.md) |
| ASP.NET Core 8 or 9 *(server side)* | [NuGet package](/reference/setup-asp-net-core.md) |


## Data access technologies [supported-data-access-technologies]

Automatic instrumentation is supported for the following data access technologies

| Data access technology | Supported versions | Integration |
| --- | --- | --- |
| Azure CosmosDB {applies_to}`apm_agent_dotnet: ga 1.11` | Microsoft.Azure.Cosmos 3.0.0-3.54.0 | [NuGet package](/reference/setup-azure-cosmosdb.md) |
| Microsoft.Azure.DocumentDB.Core 2.4.1-2.22.0 |
| Microsoft.Azure.DocumentDB 2.4.1-2.22.0 |
| Entity Framework Core {applies_to}`apm_agent_dotnet: ga 1.0` | Microsoft.EntityFrameworkCore 8.0.0-9.0.10 | [NuGet package](/reference/setup-ef-core.md) |
| Entity Framework 6 {applies_to}`apm_agent_dotnet: ga 1.2` | EntityFramework 6.2-6.5.1 | [NuGet package](/reference/setup-ef6.md) |
| Elasticsearch | Elastic.Clients.Elasticsearch 8.0.0-9.1.11 | via OpenTelemetry Bridge |
| MySQL {applies_to}`apm_agent_dotnet: ga 1.12` | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| MongoDB | MongoDB.Driver 3.0.0-3.5.0 | [NuGet package](/reference/setup-mongo-db.md) |
| Oracle {applies_to}`apm_agent_dotnet: ga 1.12` | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| PostgreSQL {applies_to}`apm_agent_dotnet: ga 1.12` | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| Redis {applies_to}`apm_agent_dotnet: ga 1.8` | StackExchange.Redis 2.0.495-2.9.32 | [NuGet package](/reference/setup-stackexchange-redis.md) |
| SqlClient | System.Data.SqlClient 2.0.495-4.9.0 {applies_to}`apm_agent_dotnet: ga 1.8` | [NuGet package](/reference/setup-sqlclient.md) |
| See profiler documentation {applies_to}`apm_agent_dotnet: ga 1.12` | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| SQLite {applies_to}`apm_agent_dotnet: ga 1.12` | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |


## Messaging systems [supported-messaging-systems]

We support automatic instrumentation for the following messaging systems

| Messaging system | Supported versions | Integration |
| --- | --- | --- |
| Azure Service Bus {applies_to}`apm_agent_dotnet: ga 1.10` | Microsoft.Azure.ServiceBus 3.0.0-5.2.0 | [NuGet package](/reference/setup-azure-servicebus.md) |
| Azure.Messaging.ServiceBus 7.0.0-7.20.1 |
| Azure Queue Storage {applies_to}`apm_agent_dotnet: ga 1.10` | Azure.Storage.Queues 12.6.0-12.24.0 | [NuGet package](/reference/setup-azure-storage.md) |
| Kafka {applies_to}`apm_agent_dotnet: ga 1.12` | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |
| Kafka | Confluent.Kafka 2.11.1 | [NuGet package](/reference/setup-kafka.md) |
| RabbitMQ {applies_to}`apm_agent_dotnet: ga 1.12` | See profiler documentation | [Profiler auto instrumentation](/reference/setup-auto-instrumentation.md) |


## Networking client-side technologies [supported-networking-client-side-technologies]

Automatic instrumentation for networking client-side technology means an HTTP span is automatically created for each outgoing HTTP request and tracing headers are propagated.

| Framework | Supported versions | Integration |
| --- | --- | --- |
| System.Net.Http.HttpClient {applies_to}`apm_agent_dotnet: ga 1.0` | *built-in* | [part of Elastic.Apm](/reference/public-api.md#setup-http) |
| System.Net.HttpWebRequest {applies_to}`apm_agent_dotnet: ga 1.1` |


## Cloud services [supported-cloud-services]

Automatic instrumentation for the following cloud services

| Cloud service | Supported versions | Integration |
| --- | --- | --- |
| Azure CosmosDB {applies_to}`apm_agent_dotnet: ga 1.11` | Microsoft.Azure.Cosmos 3.0.0-3.54.0 | [NuGet package](/reference/setup-azure-cosmosdb.md) |
| Microsoft.Azure.DocumentDB.Core 2.4.1-2.22.0 |
| Microsoft.Azure.DocumentDB 2.4.1-2.22.0 |
| Azure Service Bus {applies_to}`apm_agent_dotnet: ga 1.10` | Microsoft.Azure.ServiceBus 3.0.0-5.2.0 | [NuGet package](/reference/setup-azure-servicebus.md) |
| Azure.Messaging.ServiceBus 7.0.0-7.20.1 |
| Azure Storage {applies_to}`apm_agent_dotnet: ga 1.10` | Azure.Storage.Blobs 12.8.0-12.26.0 | [NuGet package](/reference/setup-azure-storage.md) |
| Azure.Storage.Queues 12.6.0-12.24.0 |
| Azure.Storage.Files.Shares 12.6.0-12.24.0 |
