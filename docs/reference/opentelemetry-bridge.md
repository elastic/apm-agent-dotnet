---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/opentelemetry-bridge.html
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

# OpenTelemetry bridge [opentelemetry-bridge]

The OpenTelemetry Bridge in the Elastic .NET APM Agent bridges OpenTelemetry spans into Elastic APM transactions and spans. The Elastic APM OpenTelemetry bridge allows you to use the vendor-neutral OpenTelemetry Tracing API to manually instrument your code and have the Elastic .NET APM agent handle those API calls. This means you can use the Elastic APM agent for tracing, without any vendor lock-in from adding manual tracing using the APM agent’s own [Public API](/reference/public-api.md).


## Getting started [otel-getting-started]

The OpenTelemetry bridge is part of the core agent package ([`Elastic.Apm`](https://www.nuget.org/packages/Elastic.Apm)), so you don’t need to add an additional dependency.


### Disabling the OpenTelemetry Bridge [otel-enable-bridge]

The OpenTelemetry bridge is enabled out of the box starting version `1.23.0`.

This allows you to instrument code through `ActivitySource` and `StartActivity()` without any additional configuration.

If you want to disable the bridge you can disable it for now through the [OpenTelemetryBridgeEnabled](/reference/config-core.md#config-opentelemetry-bridge-enabled) configuration setting.

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


### Mixing OpenTelemetry and the Elastic APM Public API [mixing-apis]

You can also mix the Activity API with the [Public API](/reference/public-api.md), the OpenTelemetry bridge will take care of putting the spans into the right place. The advantage of this is that if you already have some libraries that you instrumented via the [Public API](/reference/public-api.md), but going forward, you’d like to use the vendor-independent OpenTelemetry API, you don’t need to replace all Public API calls in one go.

```csharp
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

The code snippet above creates a transaction with the Elastic .NET APM Agent’s [Public API](/reference/public-api.md). Then it creates an activity called `foo`; this activity will be a child of the previously created transaction. Finally, a span is created again using the Elastic .NET APM Agent’s [Public API](/reference/public-api.md); this span will be a child span of the OpenTelemetry span.

Of course these calls don’t have to be in the same method. The concept described here works across different methods, types, or libraries.


### Baggage support [baggage-api]

The Elastic APM Agent also integrates with [Activity.Baggage](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.baggage?view=net-6.0#system-diagnostics-activity-baggage).

Here is an example that sets a baggage value with the above API:

```csharp
_activitySource.StartActivity("MyActivity")?.AddBaggage("foo", "bar");
```

The Elastic APM Agent will automatically propagate such values according to the [W3C Baggage specification](https://www.w3.org/TR/baggage/) and `Activity.Baggage` is automatically populated based on the incoming `baggage` header.

Furthermore, the agent offers the [BaggageToAttach](/reference/config-http.md#config-baggage-to-attach) configuration to automatically attach values from `Activity.Baggage` to captured events.


### Supported OpenTelemetry implementations [supported-opentelemetry-implementations]

OpenTelemetry in .NET is implemented via the [Activity API](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-6.0) and there is an [OpenTelemetry shim](https://opentelemetry.io/docs/instrumentation/net/shim/) which follows the OpenTelemetry specification more closer. This shim is built on top of the Activity API.

The OpenTelemetry bridge in the Elastic .NET APM Agent targets the [Activity API](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-6.0). Since the [OpenTelemetry .NET shim](https://opentelemetry.io/docs/instrumentation/net/shim/) builds on top of the [Activity API](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-6.0), the shim is implicitly supported as well, although we don’t directly test it, because the Activity API is the recommended OpenTelemetry API for .NET.


## Caveats [otel-caveats]

Not all features of the OpenTelemetry API are supported.


#### Metrics [otel-metrics]

This bridge only supports the tracing API. The Metrics API is currently not supported.


#### Span Events [otel-span-events]

Span events ([`Span#addEvent()`](https://open-telemetry.github.io/opentelemetry-js-api/interfaces/span.md#addevent)) are not currently supported. Events will be silently dropped.


#### Baggage [otel-baggage]

[Propagating baggage](https://open-telemetry.github.io/opentelemetry-js-api/classes/propagationapi.md) within or outside the process is not supported. Baggage items are silently dropped.

