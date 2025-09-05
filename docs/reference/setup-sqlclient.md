---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-sqlclient.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# SqlClient [setup-sqlclient]


## Quick start [_quick_start_9]

You can enable auto instrumentation for `System.Data.SqlClient` or `Microsoft.Data.SqlClient` by referencing [`Elastic.Apm.SqlClient`](https://www.nuget.org/packages/Elastic.Apm.SqlClient) package and passing `SqlClientDiagnosticSubscriber` to the `AddElasticApm` method in case of ASP.NET Core as it shown in example:

```csharp
// Enable tracing of outgoing db requests
app.Services.AddElasticApm(new SqlClientDiagnosticSubscriber());
```

or passing `SqlClientDiagnosticSubscriber` to the `Subscribe` method and make sure that the code is called only once, otherwise the same database call could be captured multiple times:

```csharp
// Enable tracing of outgoing db requests
Agent.Subscribe(new SqlClientDiagnosticSubscriber());
```

::::{note}
Auto instrumentation  for `System.Data.SqlClient` is available for both .NET Core and .NET Framework applications, however, support of .NET Framework has one limitation: command text cannot be captured.

Auto instrumentation for `Microsoft.Data.SqlClient` is available only for .NET Core at the moment.

As an alternative to using the `Elastic.Apm.SqlClient` package to instrument database calls, see [Profiler Auto instrumentation](/reference/setup-auto-instrumentation.md).

::::


