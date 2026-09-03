---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-cosmosdb.html
description: "How to enable Elastic APM .NET Agent instrumentation of Azure Cosmos DB operations to capture them as APM spans."
navigation_title: Azure Cosmos DB
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Azure Cosmos DB instrumentation [setup-azure-cosmosdb]


## Supported versions [_supported_versions_cosmosdb]

| Package | Supported versions |
| --- | --- |
| `Microsoft.Azure.Cosmos` | ≥3.0.0 <4.0.0 |
| `Microsoft.Azure.DocumentDB.Core` (legacy) | ≥2.4.1 <3.0.0 |
| `Microsoft.Azure.DocumentDB` (legacy) | ≥2.4.1 <3.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).

::::{note}
`Microsoft.Azure.DocumentDB.Core` and `Microsoft.Azure.DocumentDB` are deprecated. The recommended replacement is `Microsoft.Azure.Cosmos`.
::::


## Quick start [_quick_start_11]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.Azure.CosmosDb`](https://www.nuget.org/packages/Elastic.Apm.Azure.CosmosDb) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.Azure.CosmosDb
```

Subscribe to diagnostic events once at application startup:

```csharp
using Elastic.Apm;
using Elastic.Apm.Azure.CosmosDb;

Agent.Subscribe(new AzureCosmosDbDiagnosticsSubscriber());
```

HTTP-based diagnostic events from `Microsoft.Azure.Cosmos`, `Microsoft.Azure.DocumentDb`, and `Microsoft.Azure.DocumentDb.Core` are captured as DB spans. This covers **Gateway mode only**. Because `CosmosClient` defaults to **Direct mode** (TCP), most applications also need the additional setup described in [Direct mode (TCP)](#_direct_mode_cosmosdb) below to capture CRUD and query spans.


## Connection modes and instrumentation coverage [_connection_modes_cosmosdb]

`CosmosClient` supports two connection modes, which affect how operations are instrumented.

### Direct mode (TCP) [_direct_mode_cosmosdb]

`CosmosClient` defaults to **Direct mode**, which routes data-plane requests over the RNTBD TCP protocol, bypassing HTTP entirely. The `Elastic.Apm.Azure.CosmosDb` package does not observe these requests, so **CRUD and query operations produce no spans by default in Direct mode**.

To capture Direct-mode operations, enable the [Cosmos SDK's built-in OpenTelemetry support](https://learn.microsoft.com/en-us/azure/cosmos-db/sdk-observability) (requires `Microsoft.Azure.Cosmos` ≥ 3.36.0). The APM agent's [OpenTelemetry bridge](/reference/opentelemetry-bridge.md) (enabled by default) captures the resulting `Azure.Cosmos.Operation` activities as `db/cosmosdb` spans.

Set the following **once at application startup, before creating any `CosmosClient` instance**:

```csharp
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
```

You must also explicitly enable distributed tracing on the client — it is **disabled by default** in stable SDK releases:

```csharp
var client = new CosmosClient(connectionString, new CosmosClientOptions
{
    CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions
    {
        DisableDistributedTracing = false
    }
});
```

::::{note}
`Azure.Experimental.EnableActivitySource` is a process-wide AppContext switch. The "Experimental" label refers to the switch name itself, not to the underlying functionality. Microsoft may rename or enable it by default in a future SDK release. See [Azure Cosmos DB SDK observability](https://learn.microsoft.com/en-us/azure/cosmos-db/sdk-observability) for the current status.
::::

### Gateway mode (HTTP) [_gateway_mode_cosmosdb]

When `CosmosClient` is configured with `ConnectionMode.Gateway` (or when using the Cosmos emulator), data-plane requests travel over HTTPS. The `Elastic.Apm.Azure.CosmosDb` package intercepts these HTTP requests and captures them as `db/cosmosdb` spans with operation names such as `Cosmos DB Create/query document`.

No additional configuration is required beyond the quick-start steps above.

::::{warning}
**Gateway mode with the SDK activity source and this package produces duplicate spans.** This applies only when `Elastic.Apm.Azure.CosmosDb` is referenced, `Azure.Experimental.EnableActivitySource` is set (see [Direct mode](#_direct_mode_cosmosdb)), *and* `ConnectionMode.Gateway` is in use. Each operation generates one OTel bridge span (from the Cosmos SDK, named by the SDK, for example `create_item`) plus one or more HTTP-derived spans (from this package, named `Cosmos DB <operation>`). Paged queries and retried operations each produce a separate HTTP request, adding further HTTP spans. If the total number of spans in a transaction exceeds `transaction_max_spans`, later spans are dropped entirely. Because `Azure.Experimental.EnableActivitySource` is a process-wide switch, applications that mix Direct-mode and Gateway-mode clients cannot avoid Gateway duplicates while enabling Direct-mode tracing. If the duplicates are undesirable, do not set the switch when using Gateway mode; it is only required for Direct mode.
::::

