---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-mongo-db.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# MongoDB [setup-mongo-db]

## Quick start [_quick_start_14]

The [`Elastic.Apm.MongoDb`](https://www.nuget.org/packages/Elastic.Apm.MongoDb) NuGet package instruments the official `MongoDB.Driver` to capture MongoDB operations as APM spans, including the command name, target database, and duration.

::::{note}
::::


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

How you complete the setup depends on how you added the APM agent to your application:

**Using `Elastic.Apm.NetCoreAll`** (the all-in-one package for ASP.NET Core) — no further action is needed. MongoDB calls are captured automatically on every active transaction.

**Using `Elastic.Apm.MongoDb` directly** — also subscribe the diagnostics subscriber once at application startup:

```csharp
using Elastic.Apm;
using Elastic.Apm.MongoDb;

Agent.Subscribe(new MongoDbDiagnosticsSubscriber());
```

Make sure this is called only once. Calling it multiple times causes MongoDB operations to be captured as duplicate spans.

