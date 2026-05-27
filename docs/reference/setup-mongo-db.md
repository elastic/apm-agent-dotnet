---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-mongo-db.html
description: "Set up the Elastic APM .NET Agent to instrument MongoDB operations and capture them as APM spans."
navigation_title: MongoDB
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up MongoDB instrumentation [setup-mongo-db]

## Supported versions [_supported_versions_mongodb]

| Package | Supported versions |
| --- | --- |
| `MongoDB.Driver` | ≥3.0.0 <4.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).

::::{note}
`MongoDB.Driver` ≥3.7.0 natively emits OpenTelemetry spans. On runtimes where the agent's [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md) is supported, those spans are captured automatically, so no extra package is needed. On .NET Framework, the OpenTelemetry Bridge is not supported; use `Elastic.Apm.MongoDb` instead. This package is also required for `MongoDB.Driver` ≥3.0.0 <3.7.0.
::::

## Quick start [_quick_start_14]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

The [`Elastic.Apm.MongoDb`](https://www.nuget.org/packages/Elastic.Apm.MongoDb) NuGet package instruments the official `MongoDB.Driver` to capture MongoDB operations as {{product.apm}} spans, including the command name, target database, and duration.

### Step 1: Install the package if needed

If you are **not** already using `Elastic.Apm.NetCoreAll`, install [`Elastic.Apm.MongoDb`](https://www.nuget.org/packages/Elastic.Apm.MongoDb). If you are using `Elastic.Apm.NetCoreAll`, you can skip this install step and continue with Step 2.

```sh
dotnet add package Elastic.Apm.MongoDb
```

### Step 2: Configure the `MongoClient`

Register the `MongoDbEventSubscriber` when creating your `MongoClient`:

```csharp
using MongoDB.Driver;
using Elastic.Apm.MongoDb;

var settings = MongoClientSettings.FromConnectionString(mongoConnectionString);
settings.ClusterConfigurator = builder => builder.Subscribe(new MongoDbEventSubscriber());
var mongoClient = new MongoClient(settings);
```

### Step 3: Subscribe the agent

How you complete the setup depends on how you added the {{product.apm-agent-dotnet}} to your application:

**Using `Elastic.Apm.NetCoreAll`** The all-in-one package for ASP.NET Core. No further action is needed. MongoDB calls are captured automatically on every active transaction.

**Using `Elastic.Apm.MongoDb` directly**. Subscribe the diagnostics subscriber once at application startup:

```csharp
using Elastic.Apm;
using Elastic.Apm.MongoDb;

Agent.Subscribe(new MongoDbDiagnosticsSubscriber());
```

Make sure this is called only once. Calling it multiple times causes the agent to record duplicate spans for MongoDB operations.
