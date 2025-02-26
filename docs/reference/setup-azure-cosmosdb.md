---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-cosmosdb.html
---

# Azure Cosmos DB [setup-azure-cosmosdb]


## Quick start [_quick_start_11]

Instrumentation can be enabled for Azure Cosmos DB by referencing [`Elastic.Apm.Azure.CosmosDb`](https://www.nuget.org/packages/Elastic.Apm.Azure.CosmosDb) package and subscribing to diagnostic events.

```csharp
Agent.Subscribe(new AzureCosmosDbDiagnosticsSubscriber());
```

Diagnostic events from `Microsoft.Azure.Cosmos`, `Microsoft.Azure.DocumentDb`, and `Microsoft.Azure.DocumentDb.Core` are captured as DB spans.

