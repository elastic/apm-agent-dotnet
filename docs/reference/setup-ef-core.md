---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-ef-core.html
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

# Entity Framework Core [setup-ef-core]


## Quick start [_quick_start_5]

Instrumentation can be enabled for Entity Framework Core by referencing [`Elastic.Apm.EntityFrameworkCore`](https://www.nuget.org/packages/Elastic.Apm.EntityFrameworkCore) package and passing `EfCoreDiagnosticsSubscriber` to the `AddElasticApm` method in case of ASP.NET Core as following

```csharp
app.Services.AddElasticApm(new EfCoreDiagnosticsSubscriber());
```

or passing `EfCoreDiagnosticsSubscriber` to the `Subscribe` method

```csharp
Agent.Subscribe(new EfCoreDiagnosticsSubscriber());
```

Instrumentation listens for diagnostic events raised by `Microsoft.EntityFrameworkCore` 2.x+, creating database spans for executed commands.

