---
navigation_title: 'APM .NET agent'
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/index.html
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/intro.html
---

# APM .NET agent [intro]

The Elastic APM .NET Agent automatically measures the performance of your application and tracks errors. It has built-in support for the most popular frameworks, as well as a simple API which allows you to instrument any application.


## How does the Agent work? [how-it-works]

The agent auto-instruments [supported technologies](/reference/supported-technologies.md) and records interesting events, like HTTP requests and database queries. To do this, it uses built-in capabilities of the instrumented frameworks like [Diagnostic Source](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource?view=netcore-3.0), an HTTP module for IIS, or [IDbCommandInterceptor](https://docs.microsoft.com/en-us/dotnet/api/system.data.entity.infrastructure.interception.idbcommandinterceptor?view=entity-framework-6.2.0) for Entity Framework. This means that for the supported technologies, there are no code changes required beyond enabling [auto-instrumentation](/reference/set-up-apm-net-agent.md).

The Agent automatically registers callback methods for built-in Diagnostic Source events. With this, the supported frameworks trigger Agent code for relevant events to measure their duration and collect metadata, like DB statements, as well as HTTP related information, like the URL, parameters, and headers. These events, called Transactions and Spans, are sent to the APM Server. The APM Server converts them to a format suitable for Elasticsearch, and sends them to an Elasticsearch cluster. You can then use the APM app in Kibana to gain insight into latency issues and error culprits within your application.


## Additional Components [additional-components]

APM Agents work in conjunction with the [APM Server](docs-content://solutions/observability/apps/application-performance-monitoring-apm.md), [Elasticsearch](docs-content://get-started/index.md), and [Kibana](docs-content://get-started/the-stack.md). The [APM Guide](docs-content://solutions/observability/apps/application-performance-monitoring-apm.md) provides details on how these components work together, and provides a matrix outlining [Agent and Server compatibility](docs-content://solutions/observability/apps/apm-agent-compatibility.md).
