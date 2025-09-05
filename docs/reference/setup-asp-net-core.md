---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-asp-net-core.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# ASP.NET Core [setup-asp-net-core]


## Quick start [_quick_start_2]

For ASP.NET Core, once you reference the [`Elastic.Apm.NetCoreAll`](https://www.nuget.org/packages/Elastic.Apm.NetCoreAll) package, you can enable auto instrumentation by calling the `AddAllElasticApm()` extension method on the `IServiceCollection` in the `Program.cs` file.

::::{note}
The following code sample assumes the instrumentation of a ASP.NET Core 8 application, using [top-level statements](https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/top-level-statements).

::::


```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAllElasticApm();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Run();
```

With this you enable every agent component including ASP.NET Core tracing, monitoring of outgoing HTTP request, Entity Framework Core database tracing, etc.

In case you only reference the [`Elastic.Apm.AspNetCore`](https://www.nuget.org/packages/Elastic.Apm.AspNetCore) package, you won’t find the `AddAllElasticApm`. Instead you need to use the `AddElasticApmForAspNetCore()` method. This method turns on ASP.NET Core tracing, and gives you the opportunity to manually turn on other components. By default it will only trace ASP.NET Core requests - No HTTP request tracing, database call tracing or any other tracing component will be turned on.

In case you would like to turn on specific tracing components you can pass those to the `AddElasticApm` method.

For example:

```csharp
builder.Services.AddElasticApm(
	new HttpDiagnosticsSubscriber(),  /* Enable tracing of outgoing HTTP requests */
	new EfCoreDiagnosticsSubscriber()); /* Enable tracing of database calls through EF Core*/
```

In case you only want to use the [*Public API*](/reference/public-api.md), you don’t need to do any initialization, you can simply start using the API and the agent will send the data to the APM Server.

