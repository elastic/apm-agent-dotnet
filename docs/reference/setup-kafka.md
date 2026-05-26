---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-kafka.html
description: "How to enable Elastic APM .NET agent tracing of Confluent Kafka producers and consumers via OpenTelemetry activities or the profiler."
navigation_title: Confluent Kafka
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Confluent Kafka instrumentation [setup-kafka]

## Supported versions [_supported_versions_kafka]

| Package | Supported versions |
| --- | --- |
| `Confluent.Kafka` | ≥1.4.0 <3.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Messaging systems](/reference/supported-technologies.md#supported-messaging-systems).

## Quick start [_get_started]

This page assumes the core agent is already set up. If not, see [Set up the APM .NET agent](/reference/set-up-apm-net-agent.md) first.

`Confluent.Kafka` does not natively emit OpenTelemetry activities. To enable tracing without the profiler, add [`Confluent.Kafka.Extensions.Diagnostics`](https://www.nuget.org/packages/Confluent.Kafka.Extensions.Diagnostics) (a third-party package) to your project. This package wraps producers and consumers to emit OpenTelemetry activities, which the agent's built-in [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md) captures automatically.

Follow the setup instructions in the [`Confluent.Kafka.Extensions.Diagnostics`](https://www.nuget.org/packages/Confluent.Kafka.Extensions.Diagnostics) package, in particular the producer configuration which requires explicit wrapping.

::::{note}
If you are using the [Elastic APM Profiler](/reference/setup-auto-instrumentation.md), Kafka is instrumented automatically via IL rewriting for `Confluent.Kafka` ≥1.4.0 <3.0.0 with no code changes required. The `Confluent.Kafka.Extensions.Diagnostics` package is not needed in that case.
::::

