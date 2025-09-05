---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/serilog.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Serilog [serilog]

We offer a [Serilog Enricher](https://github.com/serilog/serilog/wiki/Enrichment) that adds the trace id to every log line that is created during an active trace.

The enricher lives in the [Elastic.Apm.SerilogEnricher](https://www.nuget.org/packages/Elastic.Apm.SerilogEnricher) NuGet package.

You can enable it when you configure your Serilog logger:

```csharp
var logger = new LoggerConfiguration()
   .Enrich.WithElasticApmCorrelationInfo()
   .WriteTo.Console(outputTemplate: "[{ElasticApmTraceId} {ElasticApmTransactionId} {Message:lj} {NewLine}{Exception}")
   .CreateLogger();
```

In the code snippet above `.Enrich.WithElasticApmCorrelationInfo()` enables the enricher, which will set 2 properties for log lines that are created during a transaction:

* ElasticApmTransactionId
* ElasticApmTraceId

As you can see, in the `outputTemplate` of the Console sink these two properties are printed. Of course they can be used with any other sink.

If you want to send your logs directly to Elasticsearch you can use the [Serilog.Sinks.ElasticSearch](https://www.nuget.org/packages/Serilog.Sinks.Elasticsearch) package. Furthermore, you can pass the `EcsTextFormatter` from the   [Elastic.CommonSchema.Serilog](https://www.nuget.org/packages/Elastic.CommonSchema.Serilog) package to the Elasticsearch sink, which formats all your logs according to Elastic Common Schema (ECS) and it makes sure that the trace id ends up in the correct field.

Once you added the two packages mentioned above, you can configure your logger like this:

```csharp
Log.Logger = new LoggerConfiguration()
.Enrich.WithElasticApmCorrelationInfo()
.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
{
  CustomFormatter = new EcsTextFormatter()
})
.CreateLogger();
```

With this setup the application will send all the logs automatically to Elasticsearch and you will be able to jump from traces to logs and from logs to traces.

