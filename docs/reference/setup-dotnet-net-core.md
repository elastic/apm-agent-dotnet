---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-dotnet-net-core.html
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

# .NET Core and .NET 5+ [setup-dotnet-net-core]


## Quick start [_quick_start_3]

In .NET (Core) applications using `Microsoft.Extensions.Hosting`, the agent can be registered on the `IServiceCollection`. This applies to ASP.NET Core and to other .NET applications that depend on the hosting APIs, such as those created using the [worker services](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) template.

The simplest way to enable the agent and its instrumentations requires a reference to the [`Elastic.Apm.NetCoreAll`](https://www.nuget.org/packages/Elastic.Apm.NetCoreAll) package.

```xml
<PackageReference Include="Elastic.Apm.NetCoreAll" Version="<LATEST>" /> <1>
```

1. Replace the `<LATEST>` placeholder with the latest version of the agent available on NuGet.


::::{note}
The following code sample assumes the instrumentation of a .NET 8 worker service, using [top-level statements](https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/top-level-statements).

::::


**Program.cs**

```csharp
using WorkerServiceSample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddAllElasticApm(); <1>
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

1. Register Elastic APM before registering other IHostedServices to ensure its dependencies are initialized first.


When registering services with `AddAllElasticApm()`, an APM agent with all instrumentations is enabled. On ASP.NET Core, it’ll automatically capture incoming requests, database calls through supported technologies, outgoing HTTP requests, etc.

For other application templates, such as worker services, you must manually instrument your `BackgroundService` to identify one or more units of work that should be captured.


## Manual instrumentation using `ITracer` [_manual_instrumentation_using_itracer]

`AddAllElasticApm` adds an `ITracer` to the Dependency Injection system, which can be used in your code to manually instrument your application, using the [*Public API*](/reference/public-api.md)

**Worker.cs**

```csharp
using Elastic.Apm.Api;

namespace WorkerServiceSample
{
  public class Worker : BackgroundService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITracer _tracer;

    public Worker(IHttpClientFactory httpClientFactory, ITracer tracer)
    {
      _httpClientFactory = httpClientFactory;
      _tracer = tracer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        await _tracer.CaptureTransaction("UnitOfWork", ApiConstants.TypeApp, async () => <1>
        {
          var client = _httpClientFactory.CreateClient();
          await client.GetAsync("https://www.elastic.co", stoppingToken);
          await Task.Delay(5000, stoppingToken);
        });
      }
    }
  }
}
```

1. The `CaptureTransaction` method creates a transaction named *UnitOfWork* and type *App*. The lambda passed to it represents the unit of work that should be captured within the context of the transaction.


When this application runs, a new transaction will be captured and sent for each while loop iteration. A span named *HTTP GET* within the transaction will be created for the HTTP request to `https://www.elastic.co`. The HTTP span is captured because the NetCoreAll package enables this instrumentation automatically.


## Manual instrumentation using OpenTelemetry [_manual_instrumentation_using_opentelemetry]

As an alternative to using the Elastic APM API by injecting an `ITracer`, you can use the OpenTelemetry API to manually instrument your application. The Elastic APM agent automatically bridges instrumentations created using the OpenTelemetry API, so you can use it to create spans and transactions. In .NET, the [`Activity` API](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) can be used to instrument applications.

In the case of this sample worker service, we can update the code to prefer the OpenTelemetry API.

**Worker.cs**

```csharp
using System.Diagnostics;

namespace WorkerServiceSample
{
  public class Worker : BackgroundService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly ActivitySource ActivitySource = new("MyActivitySource"); <1>

    public Worker(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        using var activity = ActivitySource.StartActivity("UnitOfWork"); <2>
        var client = _httpClientFactory.CreateClient();
        await client.GetAsync("https://www.elastic.co", stoppingToken);
        await Task.Delay(5000, stoppingToken);
      }
    }
  }
}
```

1. Defines an `ActivitySource` for this application from which activities can be created.
2. Starts an `Activity` with the name `UnitOfWork`. As this is `IDisposable`, it will automatically end when each iteration of the  `while` block ends.



## Instrumentation modules [_instrumentation_modules]

The `Elastic.Apm.NetCoreAll` package references every agent component that can be automatically configured. This is usually not a problem, but if you want to keep dependencies minimal, you can instead reference the `Elastic.Apm.Extensions.Hosting` package and register services with `AddElasticApm` method, instead of `AddAllElasticApm`. With this setup you can explicitly control what the agent will listen for.

The following example only turns on outgoing HTTP monitoring (so, for instance, database and Elasticsearch calls won’t be automatically captured):

```csharp
using Elastic.Apm.DiagnosticSource;
using WorkerServiceSample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddElasticApm(new HttpDiagnosticsSubscriber()); <1>
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

1. The `HttpDiagnosticsSubscriber` is a diagnostic listener that captures spans for outgoing HTTP requests.



## Zero code change setup on .NET Core and .NET 5+ ([1.7]) [zero-code-change-setup]

If you can’t or don’t want to reference NuGet packages in your application, you can use the startup hook feature to inject the agent during startup, if your application runs on .NET Core 3.0, .NET Core 3.1 or .NET 5 or newer.

To configure startup hooks

1. Download the `ElasticApmAgent_<version>.zip` file from the [Releases](https://github.com/elastic/apm-agent-dotnet/releases) page of the .NET APM Agent GitHub repository. You can find the file under Assets.
2. Unzip the zip file into a folder.
3. Set the `DOTNET_STARTUP_HOOKS` environment variable to point to the `ElasticApmAgentStartupHook.dll` file in the unzipped folder

    ```sh
    set DOTNET_STARTUP_HOOKS=<path-to-agent>\ElasticApmAgentStartupHook.dll <1>
    ```

    1. `<path-to-agent>` is the unzipped directory from step 2.

4. Start your .NET Core application in a context where the `DOTNET_STARTUP_HOOKS` environment variable is visible.

With this setup, the agent will be injected into the application during startup, enabling every instrumentation feature. Incoming requests will be automatically captured on ASP.NET Core (including gRPC).

::::{note}
Agent configuration can be controlled through environment variables when using the startup hook feature.

::::


