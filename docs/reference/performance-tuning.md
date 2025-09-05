---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/performance-tuning.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Performance tuning [performance-tuning]

There are many options available to tune agent performance. Which option to adjust depends on whether you are optimizing for speed, memory usage, bandwidth or storage.


## Sampling [performance-tuning-sampling]

The first knob to reach for when tuning the performance of the agent is [`TransactionSampleRate`](/reference/config-core.md#config-transaction-sample-rate). Adjusting the sample rate controls what percent of requests are traced. By default, the sample rate is set at `1.0`, meaning *all* requests are traced.

The sample rate will impact all four performance categories, so simply turning down the sample rate is an easy way to improve performance.

Here’s an example of setting the sample rate to 20% using [Configuration on ASP.NET Core](/reference/configuration-on-asp-net-core.md):

```js
{
    "ElasticApm": {
        "TransactionSampleRate": 0.2
    }
}
```


## Stack traces [performance-tuning-stack-traces]

In a complex application, a request may produce many spans. Capturing a stack trace for every span can result in significant memory usage. Stack traces are also captured for every error. There are several settings to adjust how stack traces are captured.


### Disable capturing stack traces [performance-tuning-disable-capturing-stack-traces]

To disable capturing stack traces (for both spans and errors), set [`StackTraceLimit`](/reference/config-stacktrace.md#config-stack-trace-limit) to `0`.


### Capture stack traces only for long running spans [performance-tuning-stack-traces-for-long-running-spans]

In its default settings, the APM agent collects a stack trace for every recorded span with duration longer than 5ms. To increase the duration threshold, set [`SpanStackTraceMinDuration`](/reference/config-stacktrace.md#config-span-stack-trace-min-duration).


### Reduce number of captured stack trace frames [performance-tuning-stack-frame-limit]

The [`StackTraceLimit`](/reference/config-stacktrace.md#config-stack-trace-limit) controls how many stack frames should be collected when a capturing stack trace.


## Disable capturing HTTP request and response headers [performance-tuning-disable-capture-headers]

Capturing HTTP request and response headers increases memory allocations, network bandwidth and disk space used by Elasticsearch. To disable capturing HTTP request and response headers, set [`CaptureHeaders`](/reference/config-http.md#config-capture-headers) to `false`.


## Increase metrics collection interval [performance-tuning-increase-metrics-collection-interval]

The .NET agent tracks certain system and application metrics. These metrics are regularly collected and sent to the APM Server and from there to Elasticsearch. You can adjust the interval for metrics collection with the setting [`MetricsInterval`](/reference/config-reporter.md#config-metrics-interval).

