This test project contains tests for the [`Elastic.Clients.Elasticsearch`](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) package, which is the "new" Elasticsearch .NET client.

[`Elastic.Clients.Elasticsearch`](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) emits OpenTelemetry compatible `Activity` instances by default, so there is no corresponding instrumentation package in the agent.
Spans emitted by this package are captured via the OpenTelemetry bridge.

The `Elastic.Apm.Elasticsearch` and the `Elastic.Apm.Elasticsearch.Tests` packages in this repository cover the "old" Elasticsearch client (which is the [`Elasticsearch.Net`](Elasticsearch.Net) package and NEST on top of it). That instrumentation is based on classic DiagnosticSource and pre-dates OpenTelemetry.