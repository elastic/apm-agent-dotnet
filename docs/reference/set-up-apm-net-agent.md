---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up the APM .NET agent [setup]

The .NET agent can be added to an application in three different ways

Profiler runtime instrumentation
:   The agent supports auto instrumentation without any code change and without any recompilation of your projects. See [Profiler Auto instrumentation](/reference/setup-auto-instrumentation.md).

NuGet packages
:   The agent ships as a set of [NuGet packages](/reference/nuget-packages.md) available on [nuget.org](https://nuget.org). You can add the Agent and specific instrumentations to a .NET application by referencing one or more of these packages and following the package documentation.

Host startup hook
:   On **.NET**, the agent supports auto instrumentation without any code change and without any recompilation of your projects. See [Zero code change setup on .NET](/reference/setup-dotnet-net-core.md#zero-code-change-setup) for more details.


::::{warning}
Native AOT is not supported. See [Supported .NET runtimes](/reference/supported-technologies.md#supported-dotnet-runtimes) for details.
::::

## Choosing an approach [choosing-an-approach]

The three approaches can be used independently or in combination. Use the guidance below to pick the right one for your situation.

| Situation | Recommended approach |
| --- | --- |
| No code changes allowed; instrumenting a third-party or legacy application | Profiler only |
| Full control over application startup; want explicit, code-level setup | NuGet packages |
| .NET Framework application using a technology not in the profiler's IL-rewriting list (for example, Entity Framework 6, Redis) | NuGet package required — the startup hook is not available on .NET Framework |
| Want zero-code-change entry point *and* richer coverage for specific libraries | Profiler + NuGet augmentation — add the relevant integration packages alongside the profiler |
| Library already emits native OpenTelemetry spans (for example, `Elastic.Clients.Elasticsearch`, MongoDB Driver ≥3.7) | Profiler or NuGet provides the [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md) automatically — no extra Elastic integration package needed |

For the full breakdown of which technologies are supported by each method, see [Supported technologies](/reference/supported-technologies.md).

## Get started [_get_started]

* [Profiler Auto instrumentation](/reference/setup-auto-instrumentation.md)
* [ASP.NET Core](/reference/setup-asp-net-core.md)
* [.NET](/reference/setup-dotnet-net-core.md)
* [ASP.NET](/reference/setup-asp-dot-net.md)
* [Azure Functions](/reference/setup-azure-functions.md)
* [Manual instrumentation](/reference/setup-general.md)

## Next: configure the agent [configure-the-agent]

Once installed, the agent needs to know where to send data. At a minimum you'll need to set your APM Server URL, an authentication credential, and optionally a service name. See [Minimum configuration](/reference/configuration.md#minimum-configuration) for the three settings that every deployment needs.
