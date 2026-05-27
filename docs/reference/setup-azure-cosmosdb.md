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

Diagnostic events from `Microsoft.Azure.Cosmos`, `Microsoft.Azure.DocumentDb`, and `Microsoft.Azure.DocumentDb.Core` are captured as DB spans.

