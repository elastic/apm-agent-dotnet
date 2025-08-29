---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-elasticsearch.html
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

# Elasticsearch [setup-elasticsearch]


## Quick start [_quick_start_7]

Instrumentation can be enabled for Elasticsearch when using the official Elasticsearch clients, Elasticsearch.Net and Nest, by referencing [`Elastic.Apm.Elasticsearch`](https://www.nuget.org/packages/Elastic.Apm.Elasticsearch) package and passing `ElasticsearchDiagnosticsSubscriber` to the `AddElasticApm` method in case of ASP.NET Core as following

```csharp
app.Services.AddElasticApm(new ElasticsearchDiagnosticsSubscriber());
```

or passing `ElasticsearchDiagnosticsSubscriber` to the `Subscribe` method

```csharp
Agent.Subscribe(new ElasticsearchDiagnosticsSubscriber());
```

Instrumentation listens for activities raised by `Elasticsearch.Net` and `Nest` 7.6.0+, creating spans for executed requests.

::::{important}
If you’re using `Elasticsearch.Net` and `Nest` 7.10.1 or 7.11.0, upgrade to at least 7.11.1 which fixes a bug in span capturing.

::::


