---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-ef6.html
description: "How to enable Elastic APM .NET Agent instrumentation of Entity Framework 6 database operations using the Ef6Interceptor."
navigation_title: Entity Framework 6
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Entity Framework 6 instrumentation [setup-ef6]


## Supported versions [_supported_versions_ef6]

| Package | Supported versions |
| --- | --- |
| `EntityFramework` | ≥6.2 ≤6.5.2 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).


## Quick start [_quick_start_6]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.EntityFramework6`](https://www.nuget.org/packages/Elastic.Apm.EntityFramework6) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.EntityFramework6
```

Include the `Ef6Interceptor` interceptor in your application’s `web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <entityFramework>
        <interceptors>
            <interceptor type="Elastic.Apm.EntityFramework6.Ef6Interceptor, Elastic.Apm.EntityFramework6" />
        </interceptors>
    </entityFramework>
</configuration>
```

As an alternative to registering the interceptor via the configuration, you can register it in the application code:

```csharp
using System.Data.Entity;

DbInterception.Add(new Elastic.Apm.EntityFramework6.Ef6Interceptor());
```

For example, in an ASP.NET application, you can place the above call in the `Application_Start` method.

Instrumentation works with EntityFramework ≥6.2 ≤6.5.2 NuGet packages.

::::{note}
Be careful not to execute `DbInterception.Add` for the same interceptor type more than once, as this will register multiple instances, causing multiple database spans to be captured for every SQL command.
::::
