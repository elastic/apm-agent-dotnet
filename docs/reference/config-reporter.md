---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-reporter.html
description: "Reference for reporter configuration options that control how the agent connects to the APM Server, including server URL, authentication, and data buffering settings."
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Reporter configuration options [config-reporter]

## `ServerUrl` [config-server-url]

The URL for your {{product.apm-server}}. The URL must be fully qualified, including protocol (`http` or `https`) and port.

::::{important}
Use of `ServerUrls` is deprecated. Use `ServerUrl`.
::::

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVER_URL` | `ElasticApm:ServerUrl` |

| Default | Type |
| --- | --- |
| `http://localhost:8200` | String |


## `SecretToken` [config-secret-token]

A string used to ensure that only your agents can send data to your {{product.apm-server}}.

Both the agents and the {{product.apm-server}} have to be configured with the same secret token. Use this setting if the {{product.apm-server}} requires a secret token, for example, when using our hosted {{product.elasticsearch}} Service on Elastic Cloud.

::::{warning}
The `SecretToken` is sent as plain-text in every request to the server, so you should also secure your communications using HTTPS. Unless you do so, your API Key could be observed by an attacker.
::::

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SECRET_TOKEN` | `ElasticApm:SecretToken` |

| Default | Type |
| --- | --- |
| `<none>` | String |


## `ApiKey` ([1.4]) [config-api-key]

A base64-encoded string used to ensure that only your agents can send data to your {{product.apm-server}}. You must have created the API key using one of the methods described in [API keys for Elastic {{product.apm}}](docs-content://solutions/observability/apm/api-keys.md).

::::{note}
This feature is fully supported in the {{product.apm-server}} versions ≥ 7.6.
::::

::::{warning}
The `APIKey` is sent as plain-text in every request to the server, so you should also secure your communications using HTTPS. Unless you do so, your API Key could be observed by an attacker.
::::

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_API_KEY` | `ElasticApm:ApiKey` |

| Default | Type |
| --- | --- |
| `<none>` | A base64-encoded string |


## `VerifyServerCert` [config-verify-server-cert]

```{applies_to}
apm_agent_dotnet: ga 1.3
```

By default, the agent verifies the SSL certificate if you use an HTTPS connection to the {{product.apm-server}}.

Verification can be turned off by changing this setting to false.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_VERIFY_SERVER_CERT` | `ElasticApm:VerifyServerCert` |

| Default | Type |
| --- | --- |
| `true` | Boolean |

::::{note}
This configuration setting has no effect on .NET Framework versions 4.6.2 to 4.7.1. Upgrade to .NET Framework 4.7.2 or newer to use this setting.
::::


## `ServerCert` [config-server-cert]

```{applies_to}
apm_agent_dotnet: ga 1.9
```

The path to a PEM-encoded certificate used for SSL/TLS by {{product.apm-server}}. Used to perform validation through certificate pinning.

This can be specified when using a certificate signed by a Certificate Authority (CA) that is not in the trust store, such as a self-signed certificate.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVER_CERT` | `ElasticApm:ServerCert` |

| Default | Type |
| --- | --- |
| `<none>` | String |

::::{note}
This configuration setting has no effect on .NET Framework versions 4.6.2 to 4.7.1. Upgrade to .NET Framework 4.7.2 or newer to use this setting.
::::


## `ServerConnectionLifetime` [config-server-connection-lifetime]

```{applies_to}
apm_agent_dotnet: ga 1.36
```

The maximum amount of time a connection to {{product.apm-server}} is reused before the agent replaces it with a new one.

Without periodic replacement, the agent keeps using the same connection for as long as it stays open. It therefore doesn't notice when the DNS name of {{product.apm-server}} starts resolving to a different address, for example after a failover or a migration, and it doesn't spread across the instances behind a load balancer that balances per connection. Replacing connections periodically makes the agent look the name up again and connect to whichever address it currently resolves to. The agent picks up a DNS change within about this value plus the time-to-live (TTL) of the DNS record.

Supports the duration suffixes `ms`, `s` and `m`. Example: `5m`. The default unit for this option is `s`.

A connection that is in use is never interrupted. A connection that is older than this value is replaced when the next request is sent.

If `ServerConnectionLifetime` is set to `0` (or `0s`, `0ms`, and so on), connections are not replaced because of their age. This is the behavior of previous agent versions on .NET 8 or later.

Setting `ServerConnectionLifetime` to a negative value (for example `-1`, `-54s`, `-89ms`, and so on) is invalid and in that case the agent uses the default value instead.

Very small values cause the agent to open a new connection, including a new TLS handshake, for nearly every request. Use the default unless you have a specific reason to change it. If you use [`UseWindowsCredentials`](/reference/config-http.md#config-use-windows-credentials) with a proxy that uses connection-based authentication (NTLM or Negotiate), each new connection repeats the authentication handshake. Increase this value if that is a concern.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVER_CONNECTION_LIFETIME` | `ElasticApm:ServerConnectionLifetime` |

| Default | Type |
| --- | --- |
| `1m` | TimeDuration |

::::{note}
On .NET Framework this option configures `ServicePoint.ConnectionLeaseTimeout`, which has applied a fixed one-minute value in previous agent versions. .NET Framework also caches DNS results for up to two minutes (`ServicePointManager.DnsRefreshTimeout`), and the setting is applied once per process, using the configuration of the first agent that connects to {{product.apm-server}}.

This option has no effect on .NET Core 3.x to .NET 7, which are served by the `netstandard2.1` build of the agent. These runtimes are out of support.
::::


## `FlushInterval` [config-flush-interval]

```{applies_to}
apm_agent_dotnet: ga 1.1
```

The maximal amount of time events are held in the queue until there is enough to send a batch. It’s possible for a batch to contain less than [`MaxBatchEventCount`](#config-max-batch-event-count) events if there are events that need to be sent out because they were held for too long. A lower value will increase the load on your {{product.apm-server}}, while a higher value can increase the memory pressure on your app. A higher value also impacts the time until transactions are indexed and searchable in {{product.elasticsearch}}.

Supports the duration suffixes `ms`, `s` and `m`. Example: `30s`. The default unit for this option is `s`.

If `FlushInterval` is set to `0` (or `0s`, `0ms`, and so on) and there’s no event sending operation still in progress, then the Agent won’t hold events in the queue and will send them immediately.

Setting `FlushInterval` to a negative value (for example `-1`, `-54s`, `-89ms`, and so on) is invalid and in that case agent uses the default value instead.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_FLUSH_INTERVAL` | `ElasticApm:FlushInterval` |

| Default | Type |
| --- | --- |
| `10s` | TimeDuration |


## `MaxBatchEventCount` [config-max-batch-event-count]

```{applies_to}
apm_agent_dotnet: ga 1.1
```

The maximum number of events to send in a batch. It’s possible for a batch to contain less then the maximum events if there are events that need to be sent out because they were held for too long (see [`FlushInterval`](#config-flush-interval)).

Setting `MaxBatchEventCount` to `0` or a negative value is invalid and the Agent will use the default value instead.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_MAX_BATCH_EVENT_COUNT` | `ElasticApm:MaxBatchEventCount` |

| Default | Type |
| --- | --- |
| 10 | Integer |


## `MaxQueueEventCount` [config-max-queue-event-count]

```{applies_to}
apm_agent_dotnet: ga 1.1
```

The maximum number of events to hold in the queue as candidates to be sent. If the queue is at its maximum capacity then the agent discards the new events until the queue has free space.

Setting `MaxQueueEventCount` to `0` or a negative value is invalid and the Agent will use the default value instead.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_MAX_QUEUE_EVENT_COUNT` | `ElasticApm:MaxQueueEventCount` |

| Default | Type |
| --- | --- |
| 1000 | Integer |


## `MetricsInterval` [config-metrics-interval]

```{applies_to}
apm_agent_dotnet: ga 1.0.0
```

The interval at which the agent sends metrics to the {{product.apm-server}}. This must be at least `1s`. Set this to `0s` to deactivate.

Supports the duration suffixes `ms`, `s` and `m`. Example: `30s`. The default unit for this option is `s`.

| Default | Type |
| --- | --- |
| `30s` | TimeDuration |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_METRICS_INTERVAL` | `ElasticApm:MetricsInterval` |


## `DisableMetrics` [config-disable-metrics]

```{applies_to}
apm_agent_dotnet: ga 1.3.0
```

This turns off the collection of certain metrics. If the name of a metric matches any of the wildcard expressions, it will not be collected. Example: `foo.*,bar.*`

You can find the name of the available metrics in [*Metrics*](/reference/metrics.md).

This option supports the wildcard `*`, which matches zero or more characters. Examples: `/foo/*/bar/*/baz*, *foo*`. Matching is case insensitive by default. Prepending an element with (?-i) makes the matching case sensitive.

| Default | Type |
| --- | --- |
| <none> | Comma separated string |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_DISABLE_METRICS` | `ElasticApm:DisableMetrics` |


## `CloudProvider` [config-cloud-provider]

```{applies_to}
apm_agent_dotnet: ga 1.7
```

Specify which cloud provider should be assumed for metadata collection. By default, the agent attempts to detect the cloud provider and, if that fails, uses trial and error to collect the metadata.

Valid options are `"auto"`, `"aws"`, `"gcp"`, `"azure"`, and `"none"`. If this config value is set to `"none"`, no cloud metadata will be collected. If set to any of `"aws"`, `"gcp"`, or `"azure"`, attempts to collect metadata will only be performed from the chosen provider.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_CLOUD_PROVIDER` | `ElasticApm:CloudProvider` |

| Default | Type |
| --- | --- |
| `auto` | String |
