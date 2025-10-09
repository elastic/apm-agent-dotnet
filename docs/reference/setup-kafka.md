---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-kafka.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Confluent Kafka [setup-kafka]

## Quick start [_get_started]

Instrumentation can be performed for Confluent Kafka by referencing [`Confluent.Kafka`](https://www.nuget.org/packages/confluent.kafka) and [`Confluent.Kafka.Extensions.Diagnostics`](https://www.nuget.org/packages/Confluent.Kafka.Extensions.Diagnostics) packages.

`Confluent.Kafka` is not instrumented automatically but `Confluent.Kafka.Extensions.Diagnostics` provides instrumentations on top of `Confluent.Kafka`.

Please, follow the instructions provided in [`Confluent.Kafka.Extensions.Diagnostics`](https://www.nuget.org/packages/Confluent.Kafka.Extensions.Diagnostics), especially for the `Producer`.

