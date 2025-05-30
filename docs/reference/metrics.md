---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/metrics.html
---

# Metrics [metrics]

The .NET agent tracks certain system and application metrics. Some of them have built-in visualizations and some can only be visualized with custom Kibana dashboards.

These metrics will be sent regularly to the APM Server and from there to Elasticsearch. You can adjust the interval with the setting [`MetricsInterval`](/reference/config-reporter.md#config-metrics-interval).

The metrics will be stored in the `apm-*` index and have the `processor.event` property set to `metric`.

"Platform: all" means that the metric is available on every platform where .NET Core is supported.


## System metrics [metrics-system]

As of APM version 6.6, these metrics will be visualized in the APM app.

::::{important}
System CPU usage metric is collected using Performance Counters on Windows. The account under which a traced application runs must be part of the **Performance Monitor Users** group to be able to access performance counter values.

An account can be added to the **Performance Monitor Users** group from the command line

```sh
net localgroup "Performance Monitor Users" "<Account Name>" /add <1>
```

1. `<Account Name>` is the account under which the traced application runs


For applications running in IIS, [IIS application pool identities use *virtual* accounts](https://docs.microsoft.com/en-us/iis/manage/configuring-security/application-pool-identities) with a name following the convention `IIS APPPOOL\<Application pool name>`. An individual application pool identity can be added to the **Performance Monitor Users** group using the `net localgroup` command above.

::::


For more system metrics, consider installing [metricbeat](beats://reference/metricbeat/index.md) on your hosts.

**`system.cpu.total.norm.pct`**
:   type: scaled_float

format: percent

platform: Windows and Linux only

The percentage of CPU time in states other than Idle and IOWait, normalized by the number of cores.


**`system.process.cpu.total.norm.pct`**
:   type: scaled_float

format: percent

platform: all

The percentage of CPU time spent by the process since the last event. This value is normalized by the number of CPU cores and it ranges from 0 to 100%.


**`system.memory.total`**
:   type: long

format: bytes

Platform: Windows and Linux only.

Total memory.


**`system.memory.actual.free`**
:   type: long

format: bytes

Platform: Windows and Linux only.

Actual free memory.


**`system.process.memory.size`**
:   type: long

format: bytes

platform: all

The total virtual memory the process has.


**`system.process.memory.rss.bytes`**
:   type: long

format: bytes

platform: all

The total physical memory the process has.



## Runtime metrics [metrics-runtime]

**`clr.gc.count`**
:   type: long

Platform: all.

The total number of GC collections that have occurred.


**`clr.gc.gen0size`**
:   type: long

format: bytes

Platform: all.

The size of the generation 0 heap.


**`clr.gc.gen1size`**
:   type: long

format: bytes

Platform: all.

The size of the generation 1 heap.


**`clr.gc.gen2size`**
:   type: long

format: bytes

Platform: all.

The size of the generation 2 heap.


**`clr.gc.gen3size`**
:   type: long

format: bytes

Platform: all.

The size of the generation 3 heap - also known as Large Object Heap (LOH).


**`clr.gc.time`**
:   type: long

format: ms

Platform: all.

The approximate accumulated collection elapsed time in milliseconds.



## Built-in application metrics [metrics-application]

To power the [Time spent by span type](docs-content://solutions/observability/apm/transactions-ui.md) graph, the agent collects summarized metrics about the timings of spans and transactions, broken down by span type.

**`transaction.duration`**
:   type: simple timer

This timer tracks the duration of transactions and allows for the creation of graphs displaying a weighted average.

Fields:

* `sum.us`: The sum of all transaction durations in microseconds since the last report (the delta)
* `count`: The count of all transactions since the last report (the delta)

You can filter and group by these dimensions:

* `transaction.name`: The name of the transaction
* `transaction.type`: The type of the transaction, for example `request`


**`transaction.breakdown.count`**
:   type: long

format: count (delta)

The number of transactions for which breakdown metrics (`span.self_time`) have been created. As the Java agent tracks the breakdown for both sampled and non-sampled transactions, this metric is equivalent to `transaction.duration.count`

You can filter and group by these dimensions:

* `transaction.name`: The name of the transaction
* `transaction.type`: The type of the transaction, for example `request`


**`span.self_time`**
:   type: simple timer

This timer tracks the span self-times and is the basis of the transaction breakdown visualization.

Fields:

* `sum.us`: The sum of all span self-times in microseconds since the last report (the delta)
* `count`: The count of all span self-times since the last report (the delta)

You can filter and group by these dimensions:

* `transaction.name`: The name of the transaction
* `transaction.type`: The type of the transaction, for example `request`
* `span.type`: The type of the span, for example `app`, `template` or `db`
* `span.subtype`: The sub-type of the span, for example `mysql` (optional)
