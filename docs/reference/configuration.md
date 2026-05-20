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

The Elastic APM .NET agent can be configured through environment variables or, for ASP.NET Core applications, through the standard `appsettings.json` file. Both approaches support the same options but use different key names. Environment variable names use the `ELASTIC_APM_` prefix, while `appsettings.json` keys use the `ElasticApm:` prefix.

Environment variables work with every installation method. For ASP.NET Core applications registered with `AddElasticApm()` or `AddAllElasticApm()`, the agent also plugs into the [Microsoft.Extensions.Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration) infrastructure, allowing configuration through `appsettings.json` or any other configured source. For these setups, `appsettings.json` (and other `IConfiguration` sources) take precedence over `ELASTIC_APM_*` environment variables — the agent reads `IConfiguration` first and only falls back to environment variables when no `IConfiguration` value is present. For all other installation methods, including auto-instrumentation via the profiler and standalone .NET applications, environment variables are the only configuration source.


## Minimum configuration [minimum-configuration]

For non-local deployments, configure at least two settings to connect the agent to your APM Server:

| Setting | Environment variable | `appsettings.json` key | Default |
| --- | --- | --- | --- |
| Server URL | `ELASTIC_APM_SERVER_URL` | `ElasticApm:ServerUrl` | `http://localhost:8200` |
| API Key | `ELASTIC_APM_API_KEY` | `ElasticApm:ApiKey` | *(none)* |

::::{note}
For local development against a default APM Server with no authentication, these defaults are sufficient and no configuration is required.
::::

It's also recommended to set the service name explicitly:

| Setting | Environment variable | `appsettings.json` key | Default |
| --- | --- | --- | --- |
| Service name | `ELASTIC_APM_SERVICE_NAME` | `ElasticApm:ServiceName` | Entry assembly name |

::::{tip}
`ServiceName` defaults to the name of your entry assembly. Set it explicitly if the auto-detected name is not meaningful, or if multiple services share the same binary.
::::

**Environment variables** work with every installation method:

```sh
ELASTIC_APM_SERVER_URL=https://your-apm-server:8200
ELASTIC_APM_API_KEY=your-api-key
ELASTIC_APM_SERVICE_NAME=my-dotnet-service
```

For ASP.NET Core applications registered with `AddElasticApm()` or `AddAllElasticApm()`, you can instead set these in `appsettings.json`. See [Configuration on ASP.NET Core](/reference/configuration-on-asp-net-core.md) for details.

```json
{
  "ElasticApm": {
    "ServerUrl": "https://your-apm-server:8200",
    "ApiKey": "your-api-key",
    "ServiceName": "my-dotnet-service"
  }
}
```

### Authentication [authentication]

Kibana generates an API key by default when configuring the APM integration, making [`ApiKey`](/reference/config-reporter.md#config-api-key) the recommended authentication method. If your APM Server is configured with a secret token instead, use [`SecretToken`](/reference/config-reporter.md#config-secret-token).

::::{warning}
Never commit `ApiKey` or `SecretToken` values to source control. Use your platform's secrets management (for example, Azure Key Vault, AWS Secrets Manager, or Kubernetes Secrets) to inject credentials at runtime.
::::

::::{note}
If the agent cannot reach the APM Server — for example, due to a missing or incorrect URL or authentication setting — it continues to run but cannot send data. Check the agent logs if you are not seeing data in APM. See [`LogLevel`](/reference/config-supportability.md#config-log-level) to increase log verbosity.
::::


## Configuration reference [configuration-reference]

Configuration options are documented by category. Each option lists its environment variable name, `appsettings.json` key, default value, and type.

| Category | Description |
| --- | --- |
| [Core](/reference/config-core.md) | Service identity, sampling, recording, and general behavior |
| [Reporter](/reference/config-reporter.md) | APM Server connection, authentication, and reporting intervals |
| [HTTP](/reference/config-http.md) | HTTP request capture, headers, body, and trace context propagation |
| [Messaging](/reference/config-messaging.md) | Message queue and topic instrumentation |
| [Stacktrace](/reference/config-stacktrace.md) | Stack trace collection depth and namespace filtering |
| [Supportability](/reference/config-supportability.md) | Agent log level and diagnostic settings |
| [All options summary](/reference/config-all-options-summary.md) | Quick-reference table of every option |

For platform-specific configuration setup:

- [Configuration on ASP.NET Core](/reference/configuration-on-asp-net-core.md)
- [Configuration on ASP.NET](/reference/configuration-on-asp-net.md)
- [Configuration for Windows Services](/reference/configuration-for-windows-services.md)


## Dynamic configuration [dynamic-configuration]

Configuration options that are marked with the ![dynamic config](images/dynamic-config.svg "") badge can be changed at runtime when set from a supported source.

The .NET Agent supports [Central configuration](docs-content://solutions/observability/apm/apm-agent-central-configuration.md), which allows you to fine-tune certain configurations via the APM app. This feature is enabled in the Agent by default, with [`CentralConfig` ([1.1])](/reference/config-core.md#config-central-config). The agent polls for configuration changes at the interval specified by the APM Server, defaulting to 5 minutes. Changes take effect after the next poll.
