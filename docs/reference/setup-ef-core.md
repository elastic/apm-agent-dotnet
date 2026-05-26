---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-ef-core.html
description: "How to enable Elastic APM .NET agent instrumentation of Entity Framework Core database operations to capture them as APM spans."
navigation_title: Entity Framework Core
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Entity Framework Core instrumentation [setup-ef-core]


## Supported versions [_supported_versions_efcore]

| Package | Supported versions |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | ≥8.0.0 ≤10.0.x |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).


## Quick start [_quick_start_5]

This page assumes the core agent is already set up. If not, see [Set up the APM .NET agent](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.EntityFrameworkCore`](https://www.nuget.org/packages/Elastic.Apm.EntityFrameworkCore) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.EntityFrameworkCore
```

Pass `EfCoreDiagnosticsSubscriber` to the `AddElasticApm` method in case of ASP.NET Core, as follows:

```csharp
using Elastic.Apm.EntityFrameworkCore;

app.Services.AddElasticApm(new EfCoreDiagnosticsSubscriber());
```

or passing `EfCoreDiagnosticsSubscriber` to the `Subscribe` method

```csharp
using Elastic.Apm;
using Elastic.Apm.EntityFrameworkCore;

Agent.Subscribe(new EfCoreDiagnosticsSubscriber());
```

Instrumentation listens for diagnostic events raised by `Microsoft.EntityFrameworkCore`, creating database spans for executed commands.
