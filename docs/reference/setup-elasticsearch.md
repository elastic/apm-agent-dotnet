---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-elasticsearch.html
description: "How to enable Elastic APM .NET Agent instrumentation of Elasticsearch queries for both the current and legacy .NET clients."
navigation_title: Elasticsearch
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up {{product.elasticsearch}} instrumentation [setup-elasticsearch]


## Supported versions [_supported_versions_elasticsearch]

| Package | Supported versions |
| --- | --- |
| `Elastic.Clients.{{product.elasticsearch}}` (current) | ≥8.0.0 <10.0.0 |
| `{{product.elasticsearch}}.Net` (legacy) | ≥7.6.0 <8.0.0 |
| `NEST` (legacy) | ≥7.6.0 <8.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).


## Quick start [_quick_start_7]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

### Current client

The currently supported {{product.elasticsearch}} client for .NET ships in the [Elastic.Clients.{{product.elasticsearch}}](https://www.nuget.org/packages/Elastic.Clients.{{product.elasticsearch}}) NuGet package. This package and the underlying transport are instrumented with OpenTelemetry native APIs built into .NET. These will be picked up automatically when the [OpenTelemetry Bridge](/reference/config-core.md#config-opentelemetry-bridge-enabled) feature is enabled. No additional Elastic {{product.apm-agent-dotnet}} package is required.

### Legacy clients

Add the [`Elastic.Apm.{{product.elasticsearch}}`](https://www.nuget.org/packages/Elastic.Apm.{{product.elasticsearch}}) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.{{product.elasticsearch}}
```

Pass `{{product.elasticsearch}}DiagnosticsSubscriber` to the `AddElasticApm` method in case of ASP.NET Core as follows:

```csharp
using Elastic.Apm.Elasticsearch

app.Services.AddElasticApm(new {{product.elasticsearch}}DiagnosticsSubscriber());
```

or passing `{{product.elasticsearch}}DiagnosticsSubscriber` to the `Subscribe` method

```csharp
using Elastic.Apm;
using Elastic.Apm.Elasticsearch

Agent.Subscribe(new {{product.elasticsearch}}DiagnosticsSubscriber());
```

Instrumentation listens for activities raised by `{{product.elasticsearch}}.Net` and `Nest`, creating spans for executed requests.

::::{important}
If you’re using `{{product.elasticsearch}}.Net` and `Nest` 7.10.1 or 7.11.0, upgrade to at least 7.11.1 which fixes a bug in span capturing.
::::
