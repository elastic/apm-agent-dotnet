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

This page summarizes the technologies the APM Agent for .NET supports, the package or runtime versions we test, and which installation methods work for each one. Versions beyond the listed upper bound have not been tested and are not supported, but might work.

Use this page as a compatibility matrix:

1. Find the framework or library you use.
2. Check that your package or runtime version falls within the supported range.
3. Check the installation method you plan to use: Profiler, NuGet, or OpenTelemetry Bridge.
4. Read any footnotes or notes directly below that table for important limitations or setup requirements.

If you are already using OpenTelemetry, consider the [EDOT .NET SDK](elastic-otel-dotnet://reference/edot-dotnet/index.md) for traces, metrics, and logs. It covers many of the same technologies (and more) and integrates naturally with Elastic's observability platform.

## Supported .NET runtimes [supported-dotnet-runtimes]

The APM Agent for .NET libraries and components target .NET Standard 2.0 or .NET Standard 2.1.

We support .NET runtimes<br>≤10.0.x and .NET Framework runtimes from 4.6.2 to 4.8.1 for as long as they receive active support from Microsoft per the [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) and [.NET Framework support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-framework). When Microsoft ends support for a runtime version, we do too. Issues reported against unsupported runtimes will not be acted on unless they also affect a supported runtime.

::::{warning}
Native AOT is not supported. The agent relies on reflection, runtime IL emit, and embedded libraries that are incompatible with AOT compilation. Attempting to use the agent in a Native AOT-published application will fail at runtime.
::::

::::{note}
On .NET Framework, we strongly recommend at least .NET Framework 4.7.2 because of binding issues introduced by Microsoft.
::::

## Installation methods [supported-installation-methods]

Each table below shows which installation methods apply to each technology. A checkmark (✓) means the technology is supported via that installation method for the listed version range; a cross (✗) means it is not supported via that method. Where a cell shows a version qualifier such as `(≥3.7.0)`, only that narrower range is covered by that method.

::::{note}
The **OpenTelemetry Bridge** column requires .NET 8+ and APM Server ≥7.16. A checkmark there means the technology is covered through the built-in OpenTelemetry Bridge, whether the agent was installed via the Profiler or a NuGet package.

On .NET Framework, technologies that depend on the startup hook or the OpenTelemetry Bridge need the NuGet install method instead.

If no Elastic APM NuGet package exists for that technology, such as `Elastic.Clients.Elasticsearch`, it is not supported on .NET Framework.
::::

| Column | Meaning |
| --- | --- |
| **[Profiler](/reference/setup-auto-instrumentation.md)** | Instrumented automatically by the [Elastic APM .NET Profiler](/reference/setup-auto-instrumentation.md) with no code changes. On .NET, a startup hook loads DiagnosticSource subscribers and the built-in OpenTelemetry Bridge. On .NET Framework, the profiler uses IL rewriting instead. |
| **NuGet** | Install the linked integration NuGet package alongside the core `Elastic.Apm` package and add the setup call to application startup. |
| **OpenTelemetry Bridge** | The library emits native [OpenTelemetry](https://opentelemetry.io/) spans that the agent captures through its built-in [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md). |

## Web frameworks [supported-web-frameworks]

For supported web frameworks, the agent creates one transaction per incoming request and names it after the registered route.

| Framework | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| ASP.NET Core<br>{applies_to}`apm_agent_dotnet: ga 1.0` | ≥8.0.0<br>≤10.0.x | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-asp-net-core.md) | ✗ |
| ASP.NET (.NET Framework) in IIS<br>{applies_to}`apm_agent_dotnet: ga 1.1` | 4.6.2–4.8.1 (IIS 10) | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-asp-dot-net.md) | ✗ |

¹ Via startup hook on .NET.

::::{note}
We support ASP.NET on IIS 10 versions supported by Microsoft per their [IIS support policy](https://learn.microsoft.com/lifecycle/products/internet-information-services-iis). IIS must be installed on a [supported](https://learn.microsoft.com/windows/release-health/windows-server-release-info#windows-server-major-versions-by-servicing-option--) Windows operating system version.

The profiler does not support the Web Garden (multi-worker process) mode of IIS.
::::

## RPC frameworks [supported-rpc-frameworks]

For supported gRPC frameworks, the agent automatically captures both client-side and server-side calls.

Streaming is not supported - the agent does not create transactions or spans for streaming calls automatically.

| Framework | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| gRPC server (ASP.NET Core)<br>{applies_to}`apm_agent_dotnet: ga 1.7` | ≥8.0.0<br>≤10.0.x | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-asp-net-core.md) | ✗ |
| gRPC client<br>`Grpc.Net.Client`<br>{applies_to}`apm_agent_dotnet: ga 1.7` | ≥2.23.2<br><3.0.0 | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-grpc.md) | [✓ (≥2.57.0)](/reference/opentelemetry-bridge.md) |

¹ Via startup hook on .NET.

::::{note}
`Grpc.Net.Client` ≥2.57.0 emits native OpenTelemetry spans. When using the profiler without the NuGet package, the OpenTelemetry Bridge captures them automatically. When the NuGet package is installed, the dedicated subscriber takes precedence to prevent duplicate spans.
::::

## Data access technologies [supported-data-access-technologies]

| Data access technology | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure CosmosDB<br>`Microsoft.Azure.Cosmos`<br>{applies_to}`apm_agent_dotnet: ga 1.11` | ≥3.0.0<br><4.0.0 | ✗ | [✓](/reference/setup-azure-cosmosdb.md) | ✗ |
| Azure DocumentDB.Core (legacy)<br>`Microsoft.Azure.DocumentDB.Core`<br>{applies_to}`apm_agent_dotnet: ga 1.11` | ≥2.4.1<br><3.0.0 | ✗ | [✓](/reference/setup-azure-cosmosdb.md) | ✗ |
| Azure DocumentDB (legacy)<br>`Microsoft.Azure.DocumentDB`<br>{applies_to}`apm_agent_dotnet: ga 1.11` | ≥2.4.1<br><3.0.0 | ✗ | [✓](/reference/setup-azure-cosmosdb.md) | ✗ |
| Elasticsearch<br>`Elastic.Clients.Elasticsearch`<br>{applies_to}`apm_agent_dotnet: ga 1.23` | ≥8.0.0<br><10.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | [✓](/reference/opentelemetry-bridge.md) |
| Elasticsearch (legacy)<br>`Elasticsearch.Net`<br>{applies_to}`apm_agent_dotnet: ga 1.6` | ≥7.6.0<br><8.0.0 | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-elasticsearch.md) | ✗ |
| Elasticsearch (legacy)<br>`NEST`<br>{applies_to}`apm_agent_dotnet: ga 1.6` | ≥7.6.0<br><8.0.0 | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-elasticsearch.md) | ✗ |
| Entity Framework Core<br>`Microsoft.EntityFrameworkCore`<br>{applies_to}`apm_agent_dotnet: ga 1.0` | ≥8.0.0<br>≤10.0.x | [✓ ¹](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-ef-core.md) | ✗ |
| Entity Framework 6<br>`EntityFramework`<br>{applies_to}`apm_agent_dotnet: ga 1.2` | ≥6.2<br>≤6.5.2 | ✗ | [✓](/reference/setup-ef6.md) | ✗ |
| MongoDB<br>`MongoDB.Driver`<br>{applies_to}`apm_agent_dotnet: ga 1.9` | ≥3.0.0<br><4.0.0 | [✓ (≥3.7.0)](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-mongo-db.md) | [✓ (≥3.7.0)](/reference/opentelemetry-bridge.md) |
| MySQL<br>`MySql.Data`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥6.7.0<br><9.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |
| Oracle<br>`Oracle.ManagedDataAccess`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥12.2.1100<br><22.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |
| Oracle<br>`Oracle.ManagedDataAccess.Core`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥2.0.0<br><4.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |
| PostgreSQL<br>`Npgsql`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥4.0.0<br><8.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |
| Redis<br>`StackExchange.Redis`<br>{applies_to}`apm_agent_dotnet: ga 1.8` | ≥2.0.495<br><3.0.0 | ✗ | [✓ ²](/reference/setup-stackexchange-redis.md) | ✗ |
| MS SQL<br>`System.Data.SqlClient`<br>{applies_to}`apm_agent_dotnet: ga 1.0` | ≥4.0.0<br><5.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-sqlclient.md) | ✗ |
| MS SQL<br>`Microsoft.Data.SqlClient`<br>{applies_to}`apm_agent_dotnet: ga 1.0` | ≥1.0.0<br><6.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-sqlclient.md) | ✗ |
| SQLLite<br>`Microsoft.Data.Sqlite`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥2.0.0<br><9.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |
| SQLLite<br>`System.Data.SQLite`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥1.0.0<br><3.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |

¹ Via startup hook on .NET.
² Requires calling `connection.UseElasticApm()` on each `IConnectionMultiplexer` instance - see the [setup page](/reference/setup-stackexchange-redis.md).

::::{note}
`Microsoft.Azure.DocumentDB.Core` and `Microsoft.Azure.DocumentDB` are deprecated. The recommended replacement is the `Microsoft.Azure.Cosmos` package.

`Elastic.Clients.Elasticsearch` emits native OpenTelemetry spans. The legacy (deprecated) `Elasticsearch.Net` and `NEST` clients use a `DiagnosticSource`-based subscriber instead.

`MongoDB.Driver` ≥3.7.0 emits native OpenTelemetry spans. When running without the NuGet package (profiler-only install), these are captured automatically by the OpenTelemetry Bridge.
::::

## Messaging systems [supported-messaging-systems]

| Messaging system | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure Service Bus<br>`Azure.Messaging.ServiceBus`<br>{applies_to}`apm_agent_dotnet: ga 1.10` | ≥7.0.0<br><8.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-servicebus.md) | [✓](/reference/opentelemetry-bridge.md) |
| Azure Service Bus (legacy)<br>`Microsoft.Azure.ServiceBus`<br>{applies_to}`apm_agent_dotnet: ga 1.10` | ≥3.0.0<br><6.0.0 | ✗ | [✓](/reference/setup-azure-servicebus.md) | ✗ |
| Kafka<br>`Confluent.Kafka`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥1.4.0<br><3.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | [✓ via adapter ¹](/reference/setup-kafka.md) |
| RabbitMQ<br>`RabbitMQ.Client`<br>{applies_to}`apm_agent_dotnet: ga 1.12` | ≥3.6.9<br><7.0.0 | [✓](/reference/setup-auto-instrumentation.md) | ✗ | ✗ |

¹ Requires adding [`Confluent.Kafka.Extensions.Diagnostics`](https://www.nuget.org/packages/Confluent.Kafka.Extensions.Diagnostics) which wraps producers and consumers so they emit spans for the OpenTelemetry Bridge to capture. Code changes are required - see the [setup page](/reference/setup-kafka.md).

::::{note}
`Azure.Messaging.ServiceBus` emits native OpenTelemetry spans. When using the profiler without the NuGet package, the OpenTelemetry Bridge captures them automatically. When the NuGet package is installed, the dedicated subscriber takes precedence to prevent duplicate spans.

The legacy `Microsoft.Azure.ServiceBus` package does not emit native OpenTelemetry spans and requires the NuGet package.
::::

## Azure Functions [supported-azure-functions]

For supported Azure Functions hosting models, the agent creates one transaction per HTTP-triggered invocation.

| Hosting model | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure Functions isolated worker<br>`Microsoft.Azure.Functions.Worker`<br>{applies_to}`apm_agent_dotnet: ga 1.19` | ≥2.0.0<br><3.0.0 | ✗ | [✓](/reference/setup-azure-functions.md) | ✗ |
| Azure Functions in-process<br>`Microsoft.Azure.Functions.Extensions`<br>{applies_to}`apm_agent_dotnet: ga 1.24` | ≥1.1.0<br><2.0.0 | ✗ | [✓](/reference/setup-azure-functions.md) | ✗ |

::::{note}
Only HTTP-triggered invocations are traced. System metrics are not collected because of a concern with unintentionally increasing Azure Functions costs on Consumption plans.

The isolated worker model requires .NET 8+. The in-process model is [deprecated by Microsoft](https://learn.microsoft.com/en-us/azure/azure-functions/migrate-dotnet-in-process-to-isolated) - new apps should use the isolated worker model.
::::

## Azure Storage [supported-azure-storage]

| Storage service | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| Azure Blob Storage<br>`Azure.Storage.Blobs`<br>{applies_to}`apm_agent_dotnet: ga 1.10` | ≥12.8.0<br><13.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-storage.md) | [✓](/reference/opentelemetry-bridge.md) |
| Azure Queue Storage<br>`Azure.Storage.Queues`<br>{applies_to}`apm_agent_dotnet: ga 1.10` | ≥12.6.0<br><13.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-storage.md) | [✓](/reference/opentelemetry-bridge.md) |
| Azure File Share Storage<br>`Azure.Storage.Files.Shares`<br>{applies_to}`apm_agent_dotnet: ga 1.10` | ≥12.6.0<br><13.0.0 | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/setup-azure-storage.md) | [✓](/reference/opentelemetry-bridge.md) |

::::{note}
Azure Storage SDKs emit native OpenTelemetry spans. When using the profiler without the NuGet package, the OpenTelemetry Bridge captures them automatically. When the NuGet package is installed, the dedicated subscriber takes precedence to prevent duplicate spans.
::::

## Networking client-side technologies [supported-networking-client-side-technologies]

For supported networking client-side technologies, the agent creates an HTTP span for each outgoing request and propagates tracing headers automatically.

| Framework | Supported versions | [Profiler](/reference/setup-auto-instrumentation.md) | NuGet | OpenTelemetry Bridge |
| --- | --- | :---: | :---: | :---: |
| System.Net.Http.HttpClient<br>{applies_to}`apm_agent_dotnet: ga 1.0` | *built-in (.NET)* | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/public-api.md#setup-http) | ✗ |
| System.Net.HttpWebRequest<br>{applies_to}`apm_agent_dotnet: ga 1.1` | *built-in (.NET)* | [✓](/reference/setup-auto-instrumentation.md) | [✓](/reference/public-api.md#setup-http) | ✗ |
