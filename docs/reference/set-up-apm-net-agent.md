---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup.html
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

# Set up the APM .NET agent [setup]

The .NET agent can be added to an application in three different ways

Profiler runtime instrumentation
:   The agent supports auto instrumentation without any code change and without any recompilation of your projects. See [Profiler Auto instrumentation](/reference/setup-auto-instrumentation.md).

NuGet packages
:   The agent ships as a set of [NuGet packages](/reference/nuget-packages.md) available on [nuget.org](https://nuget.org). You can add the Agent and specific instrumentations to a .NET application by referencing one or more of these packages and following the package documentation.

Host startup hook
:   On **.NET Core 3.0+ or .NET 5+**, the agent supports auto instrumentation without any code change and without any recompilation of your projects. See [Zero code change setup on .NET Core](/reference/setup-dotnet-net-core.md#zero-code-change-setup) for more details.


## Get started [_get_started]

* [Profiler Auto instrumentation](/reference/setup-auto-instrumentation.md)
* [ASP.NET Core](/reference/setup-asp-net-core.md)
* [.NET Core and .NET 5+](/reference/setup-dotnet-net-core.md)
* [ASP.NET](/reference/setup-asp-dot-net.md)
* [Azure Functions](/reference/setup-azure-functions.md)
* [Manual instrumentation](/reference/setup-general.md)
