---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-elasticsearch.html
description: "How to enable Elastic APM .NET agent instrumentation of Elasticsearch queries for both the current and legacy .NET clients."
navigation_title: Elasticsearch
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Elasticsearch instrumentation [setup-elasticsearch]


## Supported versions [_supported_versions_elasticsearch]

| Package | Supported versions |
| --- | --- |
| `Elastic.Clients.Elasticsearch` (current) | ≥8.0.0 <10.0.0 |
| `Elasticsearch.Net` (legacy) | ≥7.6.0 <8.0.0 |
| `NEST` (legacy) | ≥7.6.0 <8.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).


## Quick start [_quick_start_7]

This page assumes the core agent is already set up. If not, see [Set up the APM .NET agent](/reference/set-up-apm-net-agent.md) first.

### Current client

The currently supported Elasticsearch client for .NET ships in the [Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) NuGet package. This package and the underlying transport are instrumented with OpenTelemetry native APIs built into .NET. These will be picked up automatically when the [OpenTelemetry Bridge](/reference/config-core.md#config-opentelemetry-bridge-enabled) feature is enabled. No additional Elastic APM package is required.

### Legacy clients

Add the [`Elastic.Apm.Elasticsearch`](https://www.nuget.org/packages/Elastic.Apm.Elasticsearch) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.Elasticsearch
```

Pass `ElasticsearchDiagnosticsSubscriber` to the `AddElasticApm` method in case of ASP.NET Core as follows:

```csharp
using Elastic.Apm.Elasticsearch;

app.Services.AddElasticApm(new ElasticsearchDiagnosticsSubscriber());
```

or passing `ElasticsearchDiagnosticsSubscriber` to the `Subscribe` method

```csharp
using Elastic.Apm;
using Elastic.Apm.Elasticsearch;

Agent.Subscribe(new ElasticsearchDiagnosticsSubscriber());
```

Instrumentation listens for activities raised by `Elasticsearch.Net` and `Nest`, creating spans for executed requests.

::::{important}
If you’re using `Elasticsearch.Net` and `Nest` 7.10.1 or 7.11.0, upgrade to at least 7.11.1 which fixes a bug in span capturing.
::::
