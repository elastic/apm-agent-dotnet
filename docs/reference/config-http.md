---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-http.html
---

# HTTP configuration options [config-http]


## `CaptureBody` (performance) ([1.0.1]) [config-capture-body]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

For transactions that are HTTP requests, the agent can optionally capture the request body, e.g., POST variables. If the request has a body and this setting is disabled, the body will be shown as [REDACTED]. This option is case-insensitive.

::::{important}
To allow capturing request bodies, the agent sets `AllowSynchronousIO` to `true` on a per request basis in ASP.NET Core, since capturing can occur in synchronous code paths.

[With ASP.NET Core 3.0 onwards, `AllowSynchronousIO` is `false` by default](https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?#allowsynchronousio-disabled) because a large number of blocking synchronous I/O operations can lead to thread pool starvation, which makes the application unresponsive. If your application becomes unresponsive with this feature enabled, consider disabling capturing.

In ASP.NET (.NET Full Framework), this setting has no effect on non-buffered requests (see [HttpRequest.ReadEntityBodyMode](https://docs.microsoft.com/en-us/dotnet/api/system.web.httprequest.readentitybodymode?view=netframework-4.8)).

::::


::::{warning}
Request bodies often contain sensitive values like passwords and credit card numbers. If your service handles data like this, we advise to only enable this feature with care. Turning on body capturing can also significantly increase the overhead in terms of heap usage, network utilization, and Elasticsearch index size.

::::


Possible options are `off`, `errors`, `transactions` and `all`:

* `off` - request bodies will never be reported
* `errors` - request bodies will only be reported with errors
* `transactions` - request bodies will only be reported with request transactions
* `all` - request bodies will be reported with both errors and request transactions

This setting can be changed after the agent starts.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_CAPTURE_BODY` | `ElasticApm:CaptureBody` |

| Default | Type |
| --- | --- |
| `off` | String |


## `CaptureBodyContentTypes` (performance) ([1.0.1]) [config-capture-body-content-types]

Configures the content types to be captured.

This option supports the wildcard `*`, which matches zero or more characters. Examples: `/foo/*/bar/*/baz*`, `*foo*`. Matching is case insensitive.

This setting can be changed after the agent starts.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_CAPTURE_BODY_CONTENT_TYPES` | `ElasticApm:CaptureBodyContentTypes` |

| Default | Type |
| --- | --- |
| `application/x-www-form-urlencoded*, text/*, application/json*, application/xml*` | Comma separated string |


## `CaptureHeaders` (performance) [config-capture-headers]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

If set to `true`, the agent will capture request and response headers, including cookies.

::::{note}
Setting this to `false` reduces memory allocations, network bandwidth, and disk space used by {{es}}.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_CAPTURE_HEADERS` | `ElasticApm:CaptureHeaders` |

| Default | Type |
| --- | --- |
| `true` | Boolean |


## `TraceContinuationStrategy` (performance) ([1.17.0]) [config-trace-continuation-strategy]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Valid options: `continue`, `restart`, `restart_external`.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_TRACE_CONTINUATION_STRATEGY` | `ElasticApm:TraceContinuationStrategy` |

| Default | Type |
| --- | --- |
| `continue` | String |

The `traceparent` header of requests that are traced by the Elastic APM .NET Agent might have been added by a 3rd party component.

This situation becomes more and more common as the w3c trace context gets adopted. In such cases you may end up with traces where part of the trace is outside of Elastic APM.

In order to handle this properly, the agent offers trace continuation strategies with the following values and behavior:

* `continue`: The agent takes the `traceparent` header as it is and applies it to the new transaction.
* `restart`: The agent always creates a new trace with a new trace id. In this case the agent creates a span link in the new transaction pointing to the original `traceparent`.
* `restart_external`: The agent first checks the `tracestate` header. If the header contains the `es` vendor flag (which means the request is coming from a service monitored by an Elastic APM Agent), it’s treated as internal, otherwise (including the case when the `tracestate` header is not present) it’s treated as external. In case of external calls the agent creates a new trace with a new trace id and creates a link in the new transaction pointing to the original trace.


## `TransactionIgnoreUrls` (performance) [config-transaction-ignore-urls]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

This is used to restrict requests to certain URLs from being instrumented.

This property should be set to a comma separated string containing one or more paths.

For example, in order to ignore the URLs `/foo` and `/bar`, set the configuration value to `"/foo,/bar"`.

When an incoming HTTP request is detected, its request path will be tested against each element in this list. For example, adding `/home/index` to this list would match and remove instrumentation from the following URLs:

```txt
https://www.mycoolsite.com/home/index
http://localhost/home/index
http://whatever.com/home/index?value1=123
```

In other words, the matching always happens based on the request path—hosts and query strings are ignored.

This option supports the wildcard `*`, which matches zero or more characters. Examples: `"/foo/*/bar/*/baz*`, `*foo*"`. Matching is case insensitive by default. Prepending an element with `(?-i)` makes the matching case sensitive.

::::{note}
All errors that are captured during a request to an ignored URL are still sent to the APM Server regardless of this setting.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_TRANSACTION_IGNORE_URLS` | `ElasticApm:TransactionIgnoreUrls` |

| Default | Type |
| --- | --- |
| `/VAADIN/*, /heartbeat*, /favicon.ico, *.js, *.css, *.jpg, *.jpeg, *.png, *.gif, *.webp, *.svg, *.woff, *.woff2` | Comma separated string |

::::{note}
Changing this configuration will overwrite the default value.
::::



## `TransactionNameGroups` ([1.27.0]) [config-transaction-name-groups]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

With this option, you can group transaction names that contain dynamic parts with a wildcard expression. For example, the pattern `GET /user/*/cart` would consolidate transactions, such as `GET /users/42/cart` and `GET /users/73/cart` into a single transaction name `GET /users/*/cart`, hence reducing the transaction name cardinality.

This option supports the wildcard `*`, which matches zero or more characters. Examples: `GET /foo/*/bar/*/baz*``, `*foo*`. Matching is case insensitive by default. Prepending an element with (?-i) makes the matching case sensitive.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_TRANSACTION_NAME_GROUPS` | `ElasticApm:TransactionNameGroups` |

| Default | Type |
| --- | --- |
| `<none>` | String |


## `UseElasticTraceparentHeader` ([1.3.0]) [config-use-elastic-apm-traceparent-header]

To enable [distributed tracing](docs-content://solutions/observability/apm/traces.md), the agent adds trace context headers to outgoing HTTP requests made with the `HttpClient` type. These headers (`traceparent` and `tracestate`) are defined in the [W3C Trace Context](https://www.w3.org/TR/trace-context-1/) specification.

When this setting is `true`, the agent also adds the header `elasticapm-traceparent` for backwards compatibility with older versions of Elastic APM agents. Versions prior to `1.3.0` only read the `elasticapm-traceparent` header.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_USE_ELASTIC_TRACEPARENT_HEADER` | `ElasticApm:UseElasticTraceparentHeader` |

| Default | Type |
| --- | --- |
| `true` | Boolean |


## `UsePathAsTransactionName` ([1.27.0]) [config-use-path-as-transaction-name]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

If set to `true`, transaction names of unsupported or partially-supported frameworks will be in the form of `$method $path` instead of just `$method unknown route`.

::::{warning}
If your URLs contain path parameters like `/user/$userId`, you should be very careful when enabling this flag, as it can lead to an explosion of transaction groups. Take a look at the [`TransactionNameGroups`](#config-transaction-name-groups) option on how to mitigate this problem by grouping URLs together.
::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_USE_PATH_AS_TRANSACTION_NAME` | `ElasticApm:UsePathAsTransactionName` |

| Default | Type |
| --- | --- |
| `true` | Boolean |


## `UseWindowsCredentials` [config-use-windows-credentials]

Set this property to true when requests made by the APM agent should, if requested by the server, be authenticated using the credentials of the currently logged on user.

This is useful when using windows authentication on a proxy, that routes APM agent requests to the APM server.

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_USE_WINDOWS_CREDENTIALS` | `ElasticApm:UseWindowsCredentials` |

| Default | Type |
| --- | --- |
| `false` | Boolean |


## `BaggageToAttach` ([1.24]) [config-baggage-to-attach]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Controls which baggage values are automatically attached to the given event (transaction, span, error). Baggage values are derived from the `baggage` header defined in the [W3C Baggage specification](https://www.w3.org/TR/baggage/). You can programmatically write and read baggage values via the [Activity API](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-6.0). For more details, see [`our documentation on how to integrate with OpenTelemetry`](/reference/opentelemetry-bridge.md#baggage-api).

| Environment variable name | IConfiguration key |
| --- | --- |
| `ELASTIC_APM_BAGGAGE_TO_ATTACH` | `ElasticApm:BaggageToAttach` |

| Default | Type |
| --- | --- |
| `*` | Comma separated string |


## `TraceContextIgnoreSampledFalse` [config-trace-context-ignore-sampled-false]

::::{important}
Use of `TraceContextIgnoreSampledFalse` is deprecated. Use `TraceContinuationStrategy` with the `restart_external` value.
::::


The agent uses the [W3C Trace Context](https://www.w3.org/TR/trace-context/) specification and standards for distributed tracing. The traceparent header from the W3C Trace Context specification defines a [sampled flag](https://www.w3.org/TR/trace-context/#sampled-flag) which is propagated from a caller service to a callee service, and determines whether a trace is sampled in the callee service. The default behavior of the agent honors the sampled flag value and behaves accordingly.

There may be cases where you wish to change the default behavior of the agent with respect to the sampled flag. By setting the `TraceContextIgnoreSampled` configuration value to `true`, the agent ignores the sampled flag of the W3C Trace Context traceparent header when it has a value of `false` **and** and there is no agent specific tracestate header value present. In ignoring the sampled flag, the agent makes a sampling decision based on the [sample rate](/reference/config-core.md#config-transaction-sample-rate). This can be useful when a caller service always sets a sampled flag value of `false`, that results in the agent never sampling any transactions.

::::{important}
.NET 5 applications set the W3C Trace Context for outgoing HTTP requests by default, but with the traceparent header sampled flag set to `false`. If a .NET 5 application has an active agent, the agent ensures that the sampled flag is propagated with the agent’s sampling decision. If a .NET 5 application does not have an active agent however, and the application calls another service that does have an active agent, the propagation of a sampled flag value of `false` results in no sampled transactions in the callee service.

If your application is called by an .NET 5 application that does not have an active agent, setting the `TraceContextIgnoreSampledFalse` configuration value to `true` instructs the agent to start a new transaction and make a sampling decision based on the [sample rate](/reference/config-core.md#config-transaction-sample-rate), when the traceparent header sampled flag has a value of `false` **and** there is no agent specific tracestate header value present.

::::


| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_TRACE_CONTEXT_IGNORE_SAMPLED_FALSE` | `ElasticApm:TraceContextIgnoreSampledFalse` |

| Default | Type |
| --- | --- |
| `false` | Boolean |
