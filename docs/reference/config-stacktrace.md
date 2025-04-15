---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-stacktrace.html
---

# Stacktrace configuration options [config-stacktrace]


### `ApplicationNamespaces` ([1.5]) [config-application-namespaces]

This is used to determine whether a stack trace frame is an in-app frame or a library frame. When defined, all namespaces that do not start with one of the values of this collection are ignored when determining error culprit.

Multiple namespaces can be configured as a comma separated list. For example: `"MyAppA, MyAppB"`.

This suppresses any configuration of `ExcludedNamespaces`.

| Default | Type |
| --- | --- |
| <empty string> | String |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_APPLICATION_NAMESPACES` | `ElasticApm:ApplicationNamespaces` |


### `ExcludedNamespaces` ([1.5]) [config-excluded-namespaces]

A list of namespaces to exclude when reading an exception StackTrace to determine the culprit.

Namespaces are checked with `string.StartsWith()`, so "System." matches all System namespaces.

| Default | Type |
| --- | --- |
| "System., Microsoft., MS., FSharp., Newtonsoft.Json, Serilog, NLog, Giraffe." | String |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_EXCLUDED_NAMESPACES` | `ElasticApm:ExcludedNamespaces` |

## `StackTraceLimit` (performance) [config-stack-trace-limit]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

Setting this to `0` disables stack trace collection. Any positive integer value will be used as the maximum number of frames to collect. Setting it to -1 means that all frames will be collected.

| Default | Type |
| --- | --- |
| `50` | Integer |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_STACK_TRACE_LIMIT` | `ElasticApm:StackTraceLimit` |

::::{note}
If you would like to disable stack trace capturing only for spans, but still capture stack traces for errors, set the [`SpanStackTraceMinDuration` (performance)](#config-span-stack-trace-min-duration) config to `-1`.
::::



### `SpanStackTraceMinDuration` (performance) [config-span-stack-trace-min-duration]

[![dynamic config](images/dynamic-config.svg "") ](/reference/configuration.md#dynamic-configuration)

In its default settings, the APM agent collects a stack trace for every recorded span with duration longer than `5ms`. While this is very helpful to find the exact place in your code that causes the span, collecting this stack trace does have some overhead. When setting this option to zero (regardless of the time unit), like `0ms`, stack traces are collected for all spans. Setting it to a positive value, e.g. `5ms`, limits stack trace collection to spans with durations equal to or longer than the given value, e.g. 5 milliseconds.

To disable stack trace collection for spans completely, set this option to a negative value, like `-1ms`.

Supports the duration suffixes `ms`, `s` and `m`. Example: `5ms`. The default unit for this option is `ms`

| Default | Type |
| --- | --- |
| `5ms` | TimeDuration |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_SPAN_STACK_TRACE_MIN_DURATION` | `ElasticApm:SpanStackTraceMinDuration` |

::::{important}
Use of `SpanFramesMinDuration` is deprecated. Use `SpanStackTraceMinDuration`.
::::



