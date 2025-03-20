---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/logs.html
---

# Logs [logs]

Elastic .NET Agent provides [Log correlation](#log-correlation-ids). The agent will automaticaly inject correlation IDs that allow navigation between logs, traces and services.

This features is part of [Application log ingestion strategies](docs-content://solutions/observability/logs/stream-application-logs.md).

The [`ecs-logging-dotnet`](ecs-dotnet://reference/index.md) library can also be used to use the [ECS logging](ecs-logging://reference/intro.md) format without an APM agent. ECS .NET logging will always provide [log correlation](#log-correlation-ids) IDs through [`System.Diagnostics.Activity`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-7.0). When paired with the APM Agent it will pick up the appropriate service correlation features too.


## Log correlation [log-correlation-ids]

The Elastic APM .NET agent provides integrations for popular logging frameworks, which take care of injecting trace ID fields into your application’s log records. Currently supported logging frameworks are:

* [Serilog](/reference/serilog.md)
* [NLog](/reference/nlog.md)

If your favorite logging framework is not already supported, there are two other options:

* Open a feature request, or contribute code, for additional support, as described in [CONTRIBUTING.md](https://github.com/elastic/apm-agent-dotnet/blob/main/CONTRIBUTING.md).
* Manually inject trace IDs into log records, as described in [Manual log correlation](/reference/log-correlation-manual.md).

Regardless of how you integrate APM with logging, you can use [Filebeat](beats://reference/filebeat/index.md) to send your logs to Elasticsearch, in order to correlate your traces and logs and link from APM to the [Logs app](docs-content://solutions/observability/logs/explore-logs.md).




