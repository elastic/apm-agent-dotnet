---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/configuration.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Configuration [configuration]

Utilize configuration options to adapt the Elastic APM agent to your needs. There are multiple configuration sources, each with different naming conventions for the property key.

By default, the agent uses environment variables. Additionally, on ASP.NET Core, the agent plugs into the [Microsoft.Extensions.Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration) infrastructure.


## Dynamic configuration [dynamic-configuration]

Configuration options that are marked with the ![dynamic config](images/dynamic-config.svg "") badge can be changed at runtime when set from a supported source.

The .NET Agent supports [Central configuration](docs-content://solutions/observability/apm/apm-agent-central-configuration.md), which allows you to fine-tune certain configurations via the APM app. This feature is enabled in the Agent by default, with [`CentralConfig` ([1.1])](/reference/config-core.md#config-central-config).











