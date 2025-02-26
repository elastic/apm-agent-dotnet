---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-supportability.html
---

# Supportability configuration options [config-supportability]


## `LogLevel` [config-log-level]

[![dynamic config](../images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Sets the logging level for the agent.

Valid options: `Critical`, `Error`, `Warning`, `Info`, `Debug`, `Trace` and `None` (`None` disables the logging).

::::{important}
The `AddElasticApm()` extension enables configuration, as is typical in an ASP.NET Core application. You must instead set the `LogLevel` for the internal APM logger under the `Logging` section of `appsettings.json`. More details, including a [sample configuration file](/reference/configuration-on-asp-net-core.md#sample-config) are available in [Configuration on ASP.NET Core](/reference/configuration-on-asp-net-core.md).
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_LOG_LEVEL` | `ElasticApm:LogLevel` |

| Default | Type |
| --- | --- |
| `Error` | String |

