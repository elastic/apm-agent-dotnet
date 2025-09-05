---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-stackexchange-redis.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# StackExchange.Redis [setup-stackexchange-redis]


## Quick start [_quick_start_10]

Instrumentation can be enabled for `StackExchange.Redis` by referencing [`Elastic.Apm.StackExchange.Redis`](https://www.nuget.org/packages/Elastic.Apm.StackExchange.Redis) package and calling the `UseElasticApm()` extension method defined in `Elastic.Apm.StackExchange.Redis`, on `IConnectionMultiplexer`

```csharp
// using Elastic.Apm.StackExchange.Redis;

var connection = await ConnectionMultiplexer.ConnectAsync("<redis connection>");
connection.UseElasticApm();
```

A callback is registered with the `IConnectionMultiplexer` to provide a profiling session for each transaction and span that captures redis commands sent with `IConnectionMultiplexer`.

