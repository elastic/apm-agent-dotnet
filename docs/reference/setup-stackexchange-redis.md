---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-stackexchange-redis.html
description: "How to enable Elastic APM .NET Agent instrumentation of Redis commands using the StackExchange.Redis client."
navigation_title: StackExchange.Redis
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up StackExchange.Redis instrumentation [setup-stackexchange-redis]


## Supported versions [supported-versions-redis]

| Package | Supported versions |
| --- | --- |
| `StackExchange.Redis` | ≥2.0.495 <3.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies).


## Quick start [redis-quick-start]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.StackExchange.Redis`](https://www.nuget.org/packages/Elastic.Apm.StackExchange.Redis) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.StackExchange.Redis
```

Call the `UseElasticApm()` extension method on your `IConnectionMultiplexer`:

```csharp
using Elastic.Apm.StackExchange.Redis;

var connection = await ConnectionMultiplexer.ConnectAsync("<redis connection>");
connection.UseElasticApm();
```

A callback is registered with the `IConnectionMultiplexer` to provide a profiling session for each transaction and span that captures redis commands sent with `IConnectionMultiplexer`.
