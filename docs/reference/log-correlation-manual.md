---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/log-correlation-manual.html
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

# Manual log correlation [log-correlation-manual]

If the agent-provided logging integrations are not suitable or not available for your application, then you can use the agent’s [API](/reference/public-api.md) to inject trace IDs manually. There are two main approaches you can take, depending on whether you are using structured or unstructured logging.


## Manual log correlation (structured) [log-correlation-manual-structured]

For correlating structured logs with traces, the following fields should be added to your logs:

* `trace.id`
* `transaction.id`

Given a transaction object, you can obtain its trace id by using the `Transaction.TraceId` property and its transaction id by using the `Transaction.Id` property.

You can also use the [Elastic.Apm.Agent.Tracer.CurrentTransaction](/reference/public-api.md#api-current-transaction) property anywhere in the code to access the currently active transaction.

```csharp
public (string traceId, string transactionId) GetTraceIds()
{
	if (!Agent.IsConfigured) return default;
	if (Agent.Tracer.CurrentTransaction == null) return default;
	return (Agent.Tracer.CurrentTransaction.TraceId, Agent.Tracer.CurrentTransaction.Id);
}
```

In case the agent is configured and there is an active transaction, the `traceId` and `transactionId` will always return the current trace and transaction ids that you can manually add to your logs. Make sure you store those in the fields `trace.id` and `transaction.id` when you send them to Elasticsearch.


## Manual log correlation (unstructured) [log-correlation-manual-unstructured]

For correlating unstructured logs (e.g. basic printf-style logging, like `Console.WriteLine`), you will need to include the trace ids in your log message, and then extract them using Filebeat.

If you already have a transaction object, then you can use the `TraceId` and `Id` properties. Both are of type `string`, so you can simply add them to the log.

```csharp
var currentTransaction = //Get Current transaction, e.g.: Agent.Tracer.CurrentTransaction;

Console.WriteLine($"ERROR [trace.id={currentTransaction.TraceId} transaction.id={currentTransaction.Id}] an error occurred");
```

This would print a log message along the lines of:

```
    ERROR [trace.id=cd04f33b9c0c35ae8abe77e799f126b7 transaction.id=cd04f33b9c0c35ae] an error occurred
```

For log correlation to work, the trace ids must be extracted from the log message and stored in separate fields in the Elasticsearch document. This can be achieved by [parsing the data by using ingest node](beats://reference/filebeat/configuring-ingest-node.md), in particular by using [the grok processor](elasticsearch://reference/enrich-processor/grok-processor.md).

```json
{
  "description": "...",
  "processors": [
    {
      "grok": {
        "field": "message",
        "patterns": [%{LOGLEVEL:log.level} \\[trace.id=%{TRACE_ID:trace.id}(?: transaction.id=%{SPAN_ID:transaction.id})?\\] %{GREEDYDATA:message}"],
        "pattern_definitions": {
          "TRACE_ID": "[0-9A-Fa-f]{32}",
          "SPAN_ID": "[0-9A-Fa-f]{16}"
        }
      }
    }
  ]
}
```

