---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-mongo-db.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
products:
  - id: cloud-serverless
  - id: observability
  - id: apm
---

# MongoDB [setup-mongo-db]


## Quick start [_quick_start_14]

Instrumentation for MongoDB works with the official MongoDb.Driver 3.0.0+ driver packages. A prerequisite for auto instrumentation is to configure the `MongoClient` with `MongoDbEventSubscriber`:

```csharp
var settings = MongoClientSettings.FromConnectionString(mongoConnectionString);

settings.ClusterConfigurator = builder => builder.Subscribe(new MongoDbEventSubscriber());
var mongoClient = new MongoClient(settings);
```

Once the above configuration is in place

* if the agent is included by referencing the `Elastic.Apm.NetCoreAll` package, it will automatically capture calls to MongoDB on every active transaction, and no further action is required.
* you can manually activate auto instrumentation from the `Elastic.Apm.MongoDb` package by calling

```csharp
Agent.Subscribe(new MongoDbDiagnosticsSubscriber());
```

::::{important}
MongoDB integration is currently supported on .NET Core and newer. Due to MongoDb.Driver assemblies not being strongly named, they cannot be used with Elastic APM’s strongly named assemblies on .NET Framework.

::::


