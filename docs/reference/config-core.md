---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-core.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Core configuration options [config-core]


## `Recording` [config-recording]

```{applies_to}
apm_agent_dotnet: ga 1.7.0
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

A Boolean specifying if the agent should be recording or not. When recording, the agent captures HTTP requests, tracks errors, and collects and sends metrics. When not recording, the agent works as a noop, where it does not collect data or communicate with the APM server, except for polling the central configuration endpoint. This is a reversible switch, so the agent threads are not killed when deactivated, but they will be mostly idle in this state, so the overhead should be negligible.

Use this setting to dynamically disable Elastic APM at runtime.

::::{warning}
Setting `Recording` to `false` influences the behavior of the [*Public API*](/reference/public-api.md). When the agent is not active, it won’t keep track of transactions, spans, and any related properties.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_RECORDING` | `ElasticApm:Recording` |

| Default | Type |
| --- | --- |
| `true` | Boolean |


## `Enabled` [config-enabled]

```{applies_to}
apm_agent_dotnet: ga 1.7.0
```

Setting this to `false` will completely disable the agent, including instrumentation and remote config polling. If you want to dynamically change the status of the agent, use [`recording`](#config-recording) instead.

::::{warning}
Setting `Enabled` to `false` influences the behavior of the [*Public API*](/reference/public-api.md). When the agent is not active, it won’t keep track of transactions, spans, and any related properties.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_ENABLED` | `ElasticApm:Enabled` |

| Default | Type |
| --- | --- |
| `true` | Boolean |


## `ServiceName` [config-service-name]

This is used to keep all the errors and transactions of your service together and is the primary filter in the Elastic APM user interface.

::::{note}
The service name must conform to this regular expression: `^[a-zA-Z0-9 _-]+$`. In other words, your service name must only contain characters from the ASCII alphabet, numbers, dashes, underscores, and spaces. Characters in service names that don’t match the regular expression will be replaced with the `_` symbol.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVICE_NAME` | `ElasticApm:ServiceName` |

| Default | Type |
| --- | --- |
| Name of the entry assembly | String |


## `ServiceNodeName` [config-service-node-name]

```{applies_to}
apm_agent_dotnet: ga 1.3
```

This is an optional name used to differentiate between nodes in a service. If this is not set, data aggregations are done based on a container ID (where valid) or on the reported hostname (automatically discovered).

::::{note}
This feature requires APM Server versions >= 7.5
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVICE_NODE_NAME` | `ElasticApm:ServiceNodeName` |

| Default | Type |
| --- | --- |
| `<none>` | String |


## `ServiceVersion` [config-service-version]

A version string for the currently deployed version of the service. If you don’t version your deployments, the recommended value for this field is the commit identifier of the deployed revision, e.g. the output of `git rev-parse HEAD`.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SERVICE_VERSION` | `ElasticApm:ServiceVersion` |

| Default | Type |
| --- | --- |
| Informational version of the entry assembly | String |


## `HostName` [config-hostname]

```{applies_to}
apm_agent_dotnet: ga 1.7
```

This allows for the reported hostname to be manually specified. If this is not set, the hostname will be looked up.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_HOSTNAME` | `ElasticApm:HostName` |

| Default | Type |
| --- | --- |
| `<none>` | String |


## `Environment` [config-environment]

```{applies_to}
apm_agent_dotnet: ga 1.1
```

The name of the environment that this service is deployed in, e.g. "production" or "staging".

Environments allow you to easily filter data on a global level in the APM app. It’s important to be consistent when naming environments across agents. See [environment selector](docs-content://solutions/observability/apm/filter-data.md#apm-filter-your-data-service-environment-filter) in the Kibana UI for more information.

::::{note}
This feature is fully supported in the APM app in Kibana versions >= 7.2. You must use the query bar to filter for a specific environment in versions prior to 7.2.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_ENVIRONMENT` | `ElasticApm:Environment` |

| Default | Type |
| --- | --- |
| See note below | String |

::::{note}
On ASP.NET Core application the agent uses [EnvironmentName from IHostingEnvironment](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.ihostingenvironment.environmentname?view=aspnetcore-2.2#Microsoft_AspNetCore_Hosting_IHostingEnvironment_EnvironmentName) as default environment name.
::::



## `TransactionSampleRate` [config-transaction-sample-rate]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

By default, the agent samples every transaction (e.g. a request to your service). To reduce overhead and storage requirements, set the sample rate to a value between 0.0 and 1.0. The agent will still record the overall time and result for unsampled transactions, but no context information, labels, or spans will be recorded.

::::{note}
When parsing the value for this option, the agent doesn’t consider the current culture. It also expects that a period (`.`) is used to separate the integer and the fraction of a floating-point number.
::::


This setting can be changed after the agent starts.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_TRANSACTION_SAMPLE_RATE` | `ElasticApm:TransactionSampleRate` |

| Default | Type |
| --- | --- |
| 1.0 | Double |


## `TransactionMaxSpans` (performance) [config-transaction-max-spans]

```{applies_to}
apm_agent_dotnet: ga 1.1.1
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

This limits the amount of spans that are recorded per transaction. This is helpful when a transaction creates a very high amount of spans, for example, thousands of SQL queries. Setting an upper limit helps prevent overloading the Agent and APM server in these edge cases.

::::{note}
A value of `0` means that spans will never be collected. Setting `-1` means that spans will never be dropped. The Agent will revert to the default value if the value is set below `-1`.
::::


This setting can be changed after agent starts.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_TRANSACTION_MAX_SPANS` | `ElasticApm:TransactionMaxSpans` |

| Default | Type |
| --- | --- |
| `500` | Integer |


## `CentralConfig` [config-central-config]

```{applies_to}
apm_agent_dotnet: ga 1.1
```

If set to `true`, the agent makes periodic requests to the APM Server to fetch the latest [APM Agent configuration](docs-content://solutions/observability/apm/apm-agent-central-configuration.md).

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_CENTRAL_CONFIG` | `ElasticApm:CentralConfig` |

| Default | Type |
| --- | --- |
| true | Boolean |


## `SanitizeFieldNames` [config-sanitize-field-names]

```{applies_to}
apm_agent_dotnet: ga 1.2
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Sometimes, sanitizing, i.e., redacting sensitive data sent to Elastic APM, is necessary. This configuration accepts a comma-separated list of wildcard patterns of field names that should be sanitized. These apply to HTTP headers for requests and responses, cookies and `application/x-www-form-urlencoded` data.

::::{important}
This setting only applies to values captured automatically by the agent. If you capture the request body manually with the public API, this configuration doesn’t apply, and the agent won’t sanitize the body.
::::


The wildcard, `*`, matches zero or more characters, and matching is case insensitive by default. Prepending an element with `(?-i)` makes the matching case sensitive. Examples: `/foo/*/bar/*/baz*`, `*foo*`.

Please review the data captured by Elastic APM carefully to ensure it does not contain sensitive information. If you find sensitive data in your {{es}} index, add an additional entry to this list. Setting a value here will **overwrite** the defaults, so be sure to include the default entries as well.

::::{note}
Sensitive information should not be sent in the query string. Data in the query string is considered non-sensitive. See [owasp.org](https://www.owasp.org/index.php/Information_exposure_through_query_strings_in_url) for more information.
::::


**`Cookie` header sanitization:**

The `Cookie` header is automatically redacted for incoming HTTP request transactions. Each name-value pair from the cookie list is parsed by the agent and sanitized based on the `SanitizeFieldNames` configuration. Cookies with sensitive data in their value can be redacted by adding the cookie’s name to the comma-separated list.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_SANITIZE_FIELD_NAMES` | `ElasticApm:SanitizeFieldNames` |

| Default | Type |
| --- | --- |
| `password, passwd, pwd, secret, *key, *token*, *session*, *credit*, *card*, *auth*, set-cookie, *principal*` | Comma separated string |


## `GlobalLabels` [config-global-labels]

```{applies_to}
apm_agent_dotnet: ga 1.2
```

Labels are added to all events with the format `key=value[,key=value[,...]]`. Any labels set by the application via the agent’s public API will override global labels with the same keys.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_GLOBAL_LABELS` | `ElasticApm:GlobalLabels` |

| Default | Type |
| --- | --- |
| <empty map> | Map of string to string |

::::{note}
This option requires APM Server 7.2 or later. It will have no effect on older versions.
::::



## `SpanCompressionEnabled` [config-span-compression-enabled]

```{applies_to}
apm_agent_dotnet: ga 1.14
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Setting this option to true will enable span compression feature. Span compression reduces the collection, processing, and storage overhead, and removes clutter from the UI. The tradeoff is that some information such as DB statements of all the compressed spans will not be collected.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_SPAN_COMPRESSION_ENABLED` | `ElasticApm:SpanCompressionEnabled` |

| Default | Type |
| --- | --- |
| `true` | Boolean |


## `SpanCompressionExactMatchMaxDuration` [config-span-compression-exact-match-max-duration]

```{applies_to}
apm_agent_dotnet: ga 1.14
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Consecutive spans that are exact match and that are under this threshold will be compressed into a single composite span. This option does not apply to composite spans. This reduces the collection, processing, and storage overhead, and removes clutter from the UI. The tradeoff is that the DB statements of all the compressed spans will not be collected.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_SPAN_COMPRESSION_EXACT_MATCH_MAX_DURATION` | `ElasticApm:SpanCompressionExactMatchMaxDuration` |

| Default | Type |
| --- | --- |
| `50ms` | TimeDuration |


## `SpanCompressionSameKindMaxDuration` [config-span-compression-same-kind-max-duration]

```{applies_to}
apm_agent_dotnet: ga 1.14
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Consecutive spans to the same destination that are under this threshold will be compressed into a single composite span. This option does not apply to composite spans. This reduces the collection, processing, and storage overhead, and removes clutter from the UI. The tradeoff is that the DB statements of all the compressed spans will not be collected.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_SPAN_COMPRESSION_SAME_KIND_MAX_DURATION` | `ElasticApm:SpanCompressionSameKindMaxDuration` |

| Default | Type |
| --- | --- |
| `0ms` | TimeDuration |


## `ExitSpanMinDuration` [config-exit-span-min-duration]

```{applies_to}
apm_agent_dotnet: ga 1.14
```

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Sets the minimum duration of exit spans. Exit spans with a duration lesser than this threshold are attempted to be discarded. If the exit span is equal or greater the threshold, it should be kept. In some cases exit spans cannot be discarded. For example, spans that propagate the trace context to downstream services, such as outgoing HTTP requests, can’t be discarded. However, external calls that don’t propagate context, such as calls to a database, can be discarded using this threshold. Additionally, spans that lead to an error can’t be discarded.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_EXIT_SPAN_MIN_DURATION` | `ElasticApm:ExitSpanMinDuration` |

| Default | Type |
| --- | --- |
| `0ms` | TimeDuration |


## `OpentelemetryBridgeEnabled` [config-opentelemetry-bridge-enabled]

```{applies_to}
apm_agent_dotnet: ga 1.13
```

Setting this option to `false` will disable the [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md). This will disable the use of the vendor-neutral OpenTelemetry Tracing API (the [Activity API](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity) in .NET) to manually instrument your code, and have the Elastic .NET APM agent handle those API calls.

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_OPENTELEMETRY_BRIDGE_ENABLED` | `ElasticApm:OpentelemetryBridgeEnabled` |

| Default | Type |
| --- | --- |
| `true` | Boolean |

::::{note}
The OpenTelemetry Bridge is not supported on .NET Framework.
::::