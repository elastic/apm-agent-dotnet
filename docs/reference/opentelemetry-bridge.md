---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/opentelemetry-bridge.html
description: "How to use the OpenTelemetry bridge to instrument .NET applications with the vendor-neutral OpenTelemetry Tracing API while using the Elastic APM .NET Agent for data collection."
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# OpenTelemetry bridge [opentelemetry-bridge]

The OpenTelemetry Bridge in the Elastic {{product.apm-agent-dotnet}} bridges OpenTelemetry spans into Elastic {{product.apm}} transactions and spans. The Elastic {{product.apm-agent-dotnet}} OpenTelemetry Bridge allows you to use the vendor-neutral OpenTelemetry Tracing API to manually instrument your code and have the Elastic {{product.apm-agent-dotnet}} handle those API calls. This means you can use the Elastic {{product.apm-agent-dotnet}} for tracing, without any vendor lock-in from adding manual tracing using the {{product.apm-agent-dotnet}}s own [Public API](/reference/public-api.md).

::::{note}
The OpenTelemetry Bridge is not supported on .NET Framework.
::::


## Getting started [otel-getting-started]

The OpenTelemetry bridge is part of the core agent package ([`Elastic.Apm`](https://www.nuget.org/packages/Elastic.Apm)), so you don’t need to add an additional dependency.


### Disabling the OpenTelemetry Bridge [otel-enable-bridge]

The OpenTelemetry bridge is enabled by default since version `1.23.0`.

This allows you to instrument code through `ActivitySource` and `StartActivity()` without any additional configuration.

If you want to disable the bridge you can disable it for using the [OpenTelemetryBridgeEnabled](/reference/config-core.md#config-opentelemetry-bridge-enabled) configuration setting.

If you configured the agent via environment variables, set the `ELASTIC_APM_OPENTELEMETRY_BRIDGE_ENABLED` environment variable to `false`.

If you configured the agent via the `appsettings.json` file, then set `ElasticApm:OpenTelemetryBridgeEnabled` to `false`.

```js
{
  "ElasticApm":
    {
      "ServerUrl":  "http://myapmserver:8200",
      "SecretToken":  "apm-server-secret-token",
      "OpenTelemetryBridgeEnabled": false
    }
}
```


### Create an ActivitySource and start spans [create-activity-source-and-spans]

You can create OpenTelemetry spans, or in .NET terminology, you can start creating new activities via the activity source, and the agent will bridge those spans automatically.

```csharp
using System.Diagnostics;

public static void Sample()
{
	var src = new ActivitySource("Test");
	using var activity1 = src.StartActivity(nameof(Sample), ActivityKind.Server);
	Thread.Sleep(100);
	using var activity2 = src.StartActivity("foo");
	Thread.Sleep(150);
}
```

The code snippet above creates a span named `Sample` and a child span on `Sample` named `foo`. The bridge will create a transaction from `Sample` and a child span named `foo`.


### Mixing OpenTelemetry and the Elastic {{product.apm-agent-dotnet}} Public API [mixing-apis]

You can also mix the Activity API with the [Public API](/reference/public-api.md), the OpenTelemetry bridge will take care of putting the spans into the right place. The advantage of this is that if you already have some libraries that you instrumented via the [Public API](/reference/public-api.md), but going forward, you’d like to use the vendor-independent OpenTelemetry API, you don’t need to replace all Public API calls in one go.

```csharp
using System.Diagnostics;
using Elastic.Apm.Api;

/// ElasticTransaction
/// -
/// ---> OTelSpan
///           -
///           ---> ElasticSpan

var src = new ActivitySource("Test");
tracer.CaptureTransaction( nameof(Sample4), "test", t =>
{
	Thread.Sleep(100);
	using (var activity = src.StartActivity("foo"))
	{
		tracer.CurrentSpan.CaptureSpan("ElasticApmSpan", "test", () => Thread.Sleep(50));
		Thread.Sleep(150);
	}
});
```

The code snippet above creates a transaction with the Elastic {{product.apm-agent-dotnet}}s [Public API](/reference/public-api.md). Then it creates an activity called `foo`; this activity will be a child of the previously created transaction. Finally, a span is created again using theElastic {{product.apm-agent-dotnet}}s [Public API](/reference/public-api.md); this span will be a child span of the OpenTelemetry span.

Of course these calls don’t have to be in the same method. The concept described here works across different methods, types, or libraries.


### Incoming ASP.NET Core requests [otel-aspnetcore-requests]

```{applies_to}
apm_agent_dotnet: ga 1.35
```

ASP.NET Core starts an activity named `Microsoft.AspNetCore.Hosting.HttpRequestIn` for every incoming request. When the application uses the [ASP.NET Core integration](/reference/setup-asp-net-core.md) or the [Azure Functions integration](/reference/setup-azure-functions.md), that package creates the transaction for the request, so the bridge skips the activity rather than producing a duplicate.

When neither package is loaded, the bridge creates the transaction from the activity itself. The transaction continues the trace context carried in the incoming `traceparent` and `tracestate` headers, has the type `request`, and every activity started while the request is handled becomes a span beneath it. Its name depends on the runtime version:

* On .NET 10 and later the runtime can record the request method, scheme, path and server address on the activity, and the transaction is named from the method and path, for example `GET /orders/42`. The runtime only records these when the `Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData` AppContext switch is `false`, and it reads the switch once when the web host starts. The agent sets the switch to `false` when the bridge starts unless the application has set it explicitly, so this works as long as the agent starts before the web host: in `Program.cs` before the host is built, through the profiler or startup hook, or as a hosted service registered with `WebApplication.CreateBuilder`, which starts the web server after all other hosted services. If the agent starts later in your setup, set the switch yourself, for example with `<RuntimeHostConfigurationOption Include="Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData" Value="false" />` in an `ItemGroup` of the project file.
* On .NET 8 and .NET 9 the runtime records nothing on the activity, so the transaction keeps the activity name.

Whenever the activity carries the request method and path from the runtime tags above, the transaction also records the request method and URL. Both the current (`url.scheme`, `url.path`, `url.query`, `server.address`, `server.port`) and the older (`http.scheme`, `http.target`, `http.host`) semantic conventions are understood, and an error captured while the request is handled inherits them.

The bridge has no access to the `HttpContext`. Request and response details such as headers, the body, the status code and the route template, and settings which act on the request such as [`TransactionIgnoreUrls`](/reference/config-http.md#config-transaction-ignore-urls), are only available through the ASP.NET Core integration, which remains the recommended way to instrument ASP.NET Core applications.


### Baggage support [baggage-api]

The Elastic {{product.apm-agent-dotnet}} also integrates with [Activity.Baggage](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.baggage).

Here is an example that sets a baggage value with the above API:

```csharp
using System.Diagnostics;

_activitySource.StartActivity("MyActivity")?.AddBaggage("foo", "bar");
```

The Elastic {{product.apm-agent-dotnet}} will automatically propagate such values according to the [W3C Baggage specification](https://www.w3.org/TR/baggage/) and `Activity.Baggage` is automatically populated based on the incoming `baggage` header.

Furthermore, the agent offers the [BaggageToAttach](/reference/config-http.md#config-baggage-to-attach) configuration to automatically attach values from `Activity.Baggage` to captured events.


### Supported OpenTelemetry implementations [supported-opentelemetry-implementations]

OpenTelemetry in .NET is implemented via the [Activity API](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity) and there is an [OpenTelemetry shim](https://opentelemetry.io/docs/languages/dotnet/shim/) which follows the OpenTelemetry specification more closer. This shim is built on top of the Activity API.

The OpenTelemetry bridge in theElastic {{product.apm-agent-dotnet}} targets the [Activity API](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity). Since the [OpenTelemetry .NET shim](https://opentelemetry.io/docs/languages/dotnet/shim/) builds on top of the [Activity API](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity), the shim is implicitly supported as well, although we don’t directly test it, because the Activity API is the recommended OpenTelemetry API for .NET.


## Caveats [otel-caveats]

Not all features of the OpenTelemetry API are supported.

The OpenTelemetry Bridge is not supported on .NET Framework.


#### Metrics [otel-metrics]

This bridge only supports the tracing API. The Metrics API is currently not supported.


#### Span Events [otel-span-events]

Span events are not currently supported. Events will be silently dropped.


#### Runtime connection activities on .NET 9 and later [otel-runtime-infrastructure-activities]

```{applies_to}
apm_agent_dotnet: ga 1.35
```

.NET 9 introduced experimental activity sources which describe connection level plumbing:

* `Experimental.System.Net.NameResolution` (DNS lookups)
* `Experimental.System.Net.Sockets` (socket connects)
* `Experimental.System.Net.Security` (TLS handshakes)
* `Experimental.System.Net.Http.Connections` (HTTP connection setup and connection pool waits)

These sources only emit while a listener is subscribed to them, so a listener is what activates them. **The bridge does not subscribe to them by default**, which keeps the .NET default of experimental telemetry being opt in, and avoids the cost of creating those activities in applications which did not ask for them.

To capture them, set [`OpenTelemetryBridgeExperimentalSourcesEnabled`](/reference/config-core.md#config-opentelemetry-bridge-experimental-sources-enabled) to `true`:

```
ELASTIC_APM_OPENTELEMETRY_BRIDGE_EXPERIMENTAL_SOURCES_ENABLED=true
```

or, through the `appsettings.json` file:

```js
{
  "ElasticApm":
    {
      "OpenTelemetryBridgeExperimentalSourcesEnabled": true
    }
}
```

When enabled, the bridge records these as spans where they are useful, within a transaction, for example the DNS lookup and TLS handshake that a slow outgoing HTTP call had to wait for. It never promotes them to transactions. The runtime deliberately starts `ConnectionSetup` as the root of its own trace, because a connection is shared by many requests and outlives them all, so promoting these activities would create top level transactions for work such as application startup, background processes or the agent's own communication with the APM Server, which is not useful and clutters the transactions view in Kibana.

Two things are worth knowing before enabling this. Connection setup happens once per connection rather than once per request, so these spans appear only on the first request to reach a given host, and two otherwise identical transactions can have different span counts. The names and attributes of these sources are also not stable, which is what the `Experimental.` prefix indicates, so they might change between .NET releases.

#### Choosing which activity sources are bridged [otel-activity-source-filtering]

```{applies_to}
apm_agent_dotnet: ga 1.35
```

By default the bridge subscribes to every non-experimental activity source. [`OpenTelemetryBridgeAllowedActivitySources`](/reference/config-core.md#config-opentelemetry-bridge-allowed-activity-sources) and [`OpenTelemetryBridgeDeniedActivitySources`](/reference/config-core.md#config-opentelemetry-bridge-denied-activity-sources) narrow that down. Both accept a comma separated list of wildcard patterns matched against the activity source name.

The allowed list is a gate and the denied list is a veto, so a source is bridged when it matches the allowed list and is not matched by the denied list:

```
# bridge everything except one noisy source
ELASTIC_APM_OPENTELEMETRY_BRIDGE_DENIED_ACTIVITY_SOURCES=MyApp.InternalTracing

# bridge only your own instrumentation
ELASTIC_APM_OPENTELEMETRY_BRIDGE_ALLOWED_ACTIVITY_SOURCES=MyApp.*

# bridge a whole namespace apart from one source within it
ELASTIC_APM_OPENTELEMETRY_BRIDGE_ALLOWED_ACTIVITY_SOURCES=Microsoft.*
ELASTIC_APM_OPENTELEMETRY_BRIDGE_DENIED_ACTIVITY_SOURCES=Microsoft.Something.Noisy
```

Filtering is applied when the bridge decides whether to subscribe to a source, so an excluded source is never observed, and a source which only emits while a listener is attached is never even created. That also means these settings are read once when the agent starts and cannot be changed through central configuration.

ASP.NET Core hosting can fall back to a request activity with no source name when its `Microsoft.AspNetCore` source has no listeners but request logging is enabled. The bridge applies the `Microsoft.AspNetCore` source filter to that fallback too, so excluding the source also excludes its fallback request activity. Other activities with no source name are unaffected by this special handling.
