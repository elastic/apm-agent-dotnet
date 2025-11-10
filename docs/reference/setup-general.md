---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-general.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Other .NET applications [setup-general]

If you have a .NET application that is not covered in this section, you can still use the agent and instrument your application manually.

To do this, add the [Elastic.Apm](https://www.nuget.org/packages/Elastic.Apm) package to your application and use the [*Public API*](/reference/public-api.md)
or .NET [Activity](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) APIs to manually create spans and transactions.

::::{important}
For best flexibility and reduced vendor lock-in, we recommend preferring that custom instrumentation uses the [System.Diagnostics.Activity](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity) API. Code instrumented with this API will be picked up by the OpenTelemetry Bridge and is also natively 
compatible with OpenTelemetry.
::::
