---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-sqlclient.html
description: "How to enable Elastic APM .NET Agent instrumentation of SQL Server database calls using System.Data.SqlClient or Microsoft.Data.SqlClient."
navigation_title: SqlClient
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up SqlClient instrumentation [setup-sqlclient]


## Supported versions [_supported_versions_sqlclient]

| Package | Supported versions |
| --- | --- |
| `System.Data.SqlClient` | ≥4.0.0 <5.0.0 |
| `Microsoft.Data.SqlClient` | ≥1.0.0 <6.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).


## Quick start [_quick_start_9]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.SqlClient`](https://www.nuget.org/packages/Elastic.Apm.SqlClient) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.SqlClient
```

Pass `SqlClientDiagnosticSubscriber` to the `AddElasticApm` method in case of ASP.NET Core:

```csharp
using Elastic.Apm.SqlClient;

app.Services.AddElasticApm(new SqlClientDiagnosticSubscriber());
```

or passing `SqlClientDiagnosticSubscriber` to the `Subscribe` method and make sure that the code is called only once, otherwise the same database call could be captured multiple times:

```csharp
using Elastic.Apm;
using Elastic.Apm.SqlClient;

Agent.Subscribe(new SqlClientDiagnosticSubscriber());
```

::::{note}
`System.Data.SqlClient` tracing is available for both .NET and .NET Framework applications, however, support of .NET Framework has one limitation: command text cannot be captured.

`Microsoft.Data.SqlClient` tracing is available only for .NET at the moment.

As an alternative to using the `Elastic.Apm.SqlClient` package to instrument database calls, see [Profiler Auto instrumentation](/reference/setup-auto-instrumentation.md).
::::
