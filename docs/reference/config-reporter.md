---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-reporter.html
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

# Reporter configuration options [config-reporter]


## `ServerUrl` [config-server-url]

The URL for your APM Server. The URL must be fully qualified, including protocol (`http` or `https`) and port.

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

A string used to ensure that only your agents can send data to your APM server.

Both the agents and the APM server have to be configured with the same secret token. Use this setting if the APM Server requires a secret token, for example, when using our hosted {{es}} Service on Elastic Cloud.

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

A base64-encoded string used to ensure that only your agents can send data to your APM server. You must have created the API key using the APM server’s [command line tool](docs-content://solutions/observability/apm/api-keys.md).

::::{note}
This feature is fully supported in the APM Server versions >= 7.6.
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

By default, the agent verifies the SSL certificate if you use an HTTPS connection to the APM server.

Verification can be disabled by changing this setting to false.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_VERIFY_SERVER_CERT` | `ElasticApm:VerifyServerCert` |

| Default | Type |
| --- | --- |
| `true` | Boolean |

::::{note}
This configuration setting has no effect on .NET Framework versions 4.6.2-4.7.1. We recommend upgrading to .NET Framework 4.7.2 or newer to use this configuration setting.

::::



## `ServerCert` [config-server-cert]

```{applies_to}
apm_agent_dotnet: ga 1.9
```

The path to a PEM-encoded certificate used for SSL/TLS by APM server. Used to perform validation through certificate pinning.

This can be specified when using a certificate signed by a Certificate Authority (CA) that is not in the trust store, such as a self-signed certificate.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVER_CERT` | `ElasticApm:ServerCert` |

| Default | Type |
| --- | --- |
| `<none>` | String |

::::{note}
This configuration setting has no effect on .NET Framework versions 4.6.2-4.7.1. We recommend upgrading to .NET Framework 4.7.2 or newer to use this configuration setting.

::::



## `FlushInterval` [config-flush-interval]

```{applies_to}
apm_agent_dotnet: ga 1.1
```

The maximal amount of time events are held in the queue until there is enough to send a batch. It’s possible for a batch to contain less than [`MaxBatchEventCount`](#config-max-batch-event-count) events if there are events that need to be sent out because they were held for too long. A lower value will increase the load on your APM server, while a higher value can increase the memory pressure on your app. A higher value also impacts the time until transactions are indexed and searchable in Elasticsearch.

Supports the duration suffixes `ms`, `s` and `m`. Example: `30s`. The default unit for this option is `s`.

If `FlushInterval` is set to `0` (or `0s`, `0ms`, etc.) and there’s no event sending operation still in progress, then the Agent won’t hold events in the queue and will send them immediately.

Setting `FlushInterval` to a negative value (for example `-1`, `-54s`, `-89ms`, etc.) is invalid and in that case agent uses the default value instead.

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

The interval at which the agent sends metrics to the APM Server. This must be at least `1s`. Set this to `0s` to deactivate.

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

This disables the collection of certain metrics. If the name of a metric matches any of the wildcard expressions, it will not be collected. Example: `foo.*,bar.*`

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

