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

The tables below summarize the technologies the APM Agent for .NET supports, the package or runtime versions we test, and which installation methods work for each one. Versions beyond the listed upper bound have not been tested and are not supported, but may work.

If you are already using OpenTelemetry, consider the [EDOT .NET SDK](elastic-otel-dotnet://reference/edot-dotnet/index.md) for traces, metrics, and logs. It covers many of these technologies and fits naturally with Elastic's observability platform.

## Supported .NET runtimes [supported-dotnet-runtimes]

The APM Agent for .NET libraries and components target .NET Standard 2.0 or .NET Standard 2.1.

We support .NET runtimes ≤10.0.x and .NET Framework runtimes from 4.6.2 to 4.8.1 for as long as they receive active support from Microsoft per the [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) and [.NET Framework support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-framework). When Microsoft ends support for a runtime version, we do too. Issues reported against unsupported runtimes will not be acted on unless they also affect a supported runtime.

::::{note}
On .NET Framework, we strongly recommend at least .NET Framework 4.7.2 because of binding issues introduced by Microsoft.
::::

::::{warning}
Native AOT is not supported. The agent relies on reflection, runtime IL emit, and embedded libraries that are incompatible with AOT compilation. Attempting to use the agent in a Native AOT-published application will fail at runtime.
::::

## Installation methods [supported-installation-methods]

Each table below shows which installation methods apply to each technology. A checkmark (✓) means the technology is supported via that installation method for the listed version range. Where a cell shows a version qualifier such as `(≥3.7.0)`, only that narrower range is covered by that method.

::::{note}
The **OpenTelemetry Bridge** column requires .NET 8+ and APM Server ≥7.16. A checkmark there means the library's native OpenTelemetry spans are captured by the agent regardless of whether the agent was installed via the Profiler or a NuGet package — no additional Elastic integration package is needed for that coverage.

The startup hook used by the profiler on .NET 8+ is not available on .NET Framework. Technologies that depend on the startup hook or the OpenTelemetry Bridge therefore need the NuGet install method on .NET Framework. Technologies with no Elastic APM NuGet package, such as `Elastic.Clients.Elasticsearch`, are not supported on .NET Framework.
::::

| Column | Meaning |
| --- | --- |
| **[Profiler](/reference/setup-auto-instrumentation.md)** | Instrumented automatically by the [Elastic APM .NET Profiler](/reference/setup-auto-instrumentation.md) with no code changes. On .NET, a startup hook loads DiagnosticSource subscribers and the built-in OpenTelemetry Bridge. On .NET Framework, the profiler uses IL rewriting instead. |
| **NuGet** | Install the linked integration NuGet package alongside the core `Elastic.Apm` package and add the setup call to application startup. |
| **OpenTelemetry Bridge** | The library emits native [OpenTelemetry](https://opentelemetry.io/) spans that the agent captures through its built-in [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md). |
| **—** | Not supported via this installation method. |

## Web frameworks [supported-web-frameworks]

For supported web frameworks, the agent creates one transaction per incoming request and names it after the registered route.

| Framework | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| ASP.NET Core {applies_to}`apm_agent_dotnet: ga 1.0` | ≥8.0.0 ≤10.0.x | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-asp-net-core.md) | — |
| ASP.NET (.NET Framework) in IIS {applies_to}`apm_agent_dotnet: ga 1.1` | 4.6.2–4.8.1 (IIS 10) | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-asp-dot-net.md) | — |

¹ Via startup hook on .NET.

::::{note}
We support ASP.NET on IIS 10 versions supported by Microsoft per their [IIS support policy](https://learn.microsoft.com/lifecycle/products/internet-information-services-iis). IIS must be installed on a [supported](https://learn.microsoft.com/windows/release-health/windows-server-release-info#windows-server-major-versions-by-servicing-option--) Windows operating system version.

The profiler does not support the Web Garden (multi-worker process) mode of IIS.
::::

## Azure Functions [supported-azure-functions]

For supported Azure Functions hosting models, the agent creates one transaction per HTTP-triggered invocation.

| Hosting model | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure Functions isolated worker {applies_to}`apm_agent_dotnet: ga 1.19` | Microsoft.Azure.Functions.Worker ≥2.0.0 | — | [✓](/reference/setup-azure-functions.md) | — |
| Azure Functions in-process {applies_to}`apm_agent_dotnet: ga 1.24` | Microsoft.Azure.Functions.Extensions ≥1.1.0 | — | [✓](/reference/setup-azure-functions.md) | — |

::::{note}
Only HTTP-triggered invocations are traced. System metrics are not collected because of a concern with unintentionally increasing Azure Functions costs on Consumption plans.

The isolated worker model requires .NET 8+. The in-process model is [deprecated by Microsoft](https://learn.microsoft.com/en-us/azure/azure-functions/migrate-dotnet-in-process-to-isolated) — new apps should use the isolated worker model.
::::

## RPC frameworks [supported-rpc-frameworks]

For supported gRPC frameworks, the agent automatically captures both client-side and server-side calls.

Streaming is not supported — the agent does not create transactions or spans for streaming calls automatically.

| Framework | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| gRPC client {applies_to}`apm_agent_dotnet: ga 1.7` | Grpc.Net.Client ≥2.23.2 <3.0.0 | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-grpc.md) | [✓ (≥2.57.0)](/reference/opentelemetry-bridge.md) |
| gRPC server (ASP.NET Core) {applies_to}`apm_agent_dotnet: ga 1.7` | ≥8.0.0 ≤10.0.x | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-asp-net-core.md) | — |

¹ Via startup hook on .NET.

::::{note}
`Grpc.Net.Client` ≥2.57.0 emits native OpenTelemetry spans. The OpenTelemetry Bridge provides automatic coverage when using the profiler without the NuGet package. When the NuGet package is installed, the dedicated subscriber takes precedence and the bridge is suppressed to prevent duplicate spans.
::::

## Data access technologies [supported-data-access-technologies]

| Data access technology | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure CosmosDB {applies_to}`apm_agent_dotnet: ga 1.11` | Microsoft.Azure.Cosmos ≥3.0.0 <4.0.0 | — | [✓](/reference/setup-azure-cosmosdb.md) | — |
| Azure DocumentDB, legacy {applies_to}`apm_agent_dotnet: ga 1.11` | Microsoft.Azure.DocumentDB.Core\* ≥2.4.1 <3.0.0; Microsoft.Azure.DocumentDB\* ≥2.4.1 <3.0.0 | — | [✓](/reference/setup-azure-cosmosdb.md) | — |
| Elasticsearch {applies_to}`apm_agent_dotnet: ga 1.23` | Elastic.Clients.Elasticsearch ≥8.0.0 <10.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | [✓](/reference/opentelemetry-bridge.md) |
| Elasticsearch, legacy {applies_to}`apm_agent_dotnet: ga 1.6` | Elasticsearch.Net / NEST ≥7.6.0 <8.0.0 | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-elasticsearch.md) | — |
| Entity Framework Core {applies_to}`apm_agent_dotnet: ga 1.0` | Microsoft.EntityFrameworkCore ≥8.0.0 ≤10.0.x | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-ef-core.md) | — |
| Entity Framework 6 {applies_to}`apm_agent_dotnet: ga 1.2` | EntityFramework ≥6.2 ≤6.5.2 | — | [✓](/reference/setup-ef6.md) | — |
| MongoDB {applies_to}`apm_agent_dotnet: ga 1.9` | MongoDB.Driver ≥3.0.0 <4.0.0 | [✓ (≥3.7.0)](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-mongo-db.md) | [✓ (≥3.7.0)](/reference/opentelemetry-bridge.md) |
| MySQL {applies_to}`apm_agent_dotnet: ga 1.12` | MySql.Data ≥6.7.0 <9.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | — |
| Oracle.ManagedDataAccess {applies_to}`apm_agent_dotnet: ga 1.12` | Oracle.ManagedDataAccess 4.122.x | [✓](/reference/setup-auto-instrumentation.md) | — | — |
| Oracle.ManagedDataAccess.Core {applies_to}`apm_agent_dotnet: ga 1.12` | Oracle.ManagedDataAccess.Core ≥2.0.0 <4.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | — |
| PostgreSQL {applies_to}`apm_agent_dotnet: ga 1.12` | Npgsql ≥4.0.0 <8.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | — |
| Redis {applies_to}`apm_agent_dotnet: ga 1.8` | StackExchange.Redis ≥2.0.495 <3.0.0 | — | [✓ ²](/reference/setup-stackexchange-redis.md) | — |
| SqlClient {applies_to}`apm_agent_dotnet: ga 1.0` | System.Data.SqlClient ≥4.0.0 <5.0.0; Microsoft.Data.SqlClient ≥1.0.0 <6.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-sqlclient.md) | — |
| Microsoft.Data.Sqlite {applies_to}`apm_agent_dotnet: ga 1.12` | Microsoft.Data.Sqlite ≥2.0.0 <9.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | — |
| System.Data.SQLite {applies_to}`apm_agent_dotnet: ga 1.12` | System.Data.SQLite ≥1.0.0 <3.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | — |

¹ Via startup hook on .NET.
² Requires calling `connection.UseElasticApm()` on each `IConnectionMultiplexer` instance — see the [setup page](/reference/setup-stackexchange-redis.md).

::::{note}
\* `Microsoft.Azure.DocumentDB.Core` and `Microsoft.Azure.DocumentDB` are deprecated. The recommended replacement is the `Microsoft.Azure.Cosmos` package.
::::

::::{note}
`Elastic.Clients.Elasticsearch` emits native OpenTelemetry spans. The legacy `Elasticsearch.Net` and `NEST` clients use a `DiagnosticSource`-based subscriber instead.
::::

::::{note}
MongoDB.Driver ≥3.7.0 emits native OpenTelemetry spans. When running without the NuGet package (profiler-only install), these are captured automatically by the OpenTelemetry Bridge.
::::

## Messaging systems [supported-messaging-systems]

| Messaging system | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure Service Bus {applies_to}`apm_agent_dotnet: ga 1.10` | Azure.Messaging.ServiceBus ≥7.0.0 <8.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-servicebus.md) | [✓](/reference/opentelemetry-bridge.md) |
| Azure Service Bus, legacy {applies_to}`apm_agent_dotnet: ga 1.10` | Microsoft.Azure.ServiceBus ≥3.0.0 <6.0.0 | — | [✓](/reference/setup-azure-servicebus.md) | — |
| Kafka {applies_to}`apm_agent_dotnet: ga 1.12` | Confluent.Kafka ≥1.4.0 <3.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | [✓ ¹](/reference/setup-kafka.md) |
| RabbitMQ {applies_to}`apm_agent_dotnet: ga 1.12` | RabbitMQ.Client ≥3.6.9 <7.0.0 | [✓](/reference/setup-auto-instrumentation.md) | — | — |

¹ Requires [`Confluent.Kafka.Extensions.Diagnostics`](https://www.nuget.org/packages/Confluent.Kafka.Extensions.Diagnostics) to wrap producers and consumers to emit spans captured by the OpenTelemetry Bridge. Code changes are required — see the [setup page](/reference/setup-kafka.md).

::::{note}
`Azure.Messaging.ServiceBus` emits native OpenTelemetry spans. The OpenTelemetry Bridge provides automatic coverage when using the profiler without the NuGet package. When the NuGet package is installed, the dedicated subscriber takes precedence and the bridge is suppressed to prevent duplicate spans.

The legacy `Microsoft.Azure.ServiceBus` package does not emit native OpenTelemetry spans and requires the NuGet package.
::::

## Azure Storage [supported-azure-storage]

| Storage service | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure Blob Storage {applies_to}`apm_agent_dotnet: ga 1.10` | Azure.Storage.Blobs ≥12.8.0 <13.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-storage.md) | [✓](/reference/opentelemetry-bridge.md) |
| Azure Queue Storage {applies_to}`apm_agent_dotnet: ga 1.10` | Azure.Storage.Queues ≥12.6.0 <13.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-storage.md) | [✓](/reference/opentelemetry-bridge.md) |
| Azure File Share Storage {applies_to}`apm_agent_dotnet: ga 1.10` | Azure.Storage.Files.Shares ≥12.6.0 <13.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-storage.md) | [✓](/reference/opentelemetry-bridge.md) |

::::{note}
For Azure Storage services, the OpenTelemetry Bridge provides automatic coverage when using the profiler without the NuGet package. When the NuGet package is installed, the dedicated subscriber takes precedence and the bridge is suppressed to prevent duplicate spans.
::::

## Networking client-side technologies [supported-networking-client-side-technologies]

For supported networking client-side technologies, the agent creates an HTTP span for each outgoing request and propagates tracing headers automatically.

| Framework | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| System.Net.Http.HttpClient {applies_to}`apm_agent_dotnet: ga 1.0` | *built-in (.NET)* | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/public-api.md#setup-http) | — |
| System.Net.HttpWebRequest {applies_to}`apm_agent_dotnet: ga 1.1` | *built-in (.NET)* | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/public-api.md#setup-http) | — |
