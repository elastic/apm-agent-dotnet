---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-servicebus.html
description: "How to enable Elastic APM .NET agent instrumentation of Azure Service Bus messaging operations for both the current and legacy SDK versions."
navigation_title: Azure Service Bus
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Azure Service Bus instrumentation [setup-azure-servicebus]


## Supported versions [_supported_versions_servicebus]

| SDK | Supported versions |
| --- | --- |
| `Azure.Messaging.ServiceBus` (current) | ≥7.0.0 <8.0.0 |
| `Microsoft.Azure.ServiceBus` (legacy, deprecated) | ≥3.0.0 <6.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Messaging systems](/reference/supported-technologies.md#supported-messaging-systems).


## Quick start [_quick_start_12]

This page assumes the core agent is already set up. If not, see [Set up the APM .NET agent](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.Azure.ServiceBus`](https://www.nuget.org/packages/Elastic.Apm.Azure.ServiceBus) NuGet package to your project:

```bash
dotnet add package Elastic.Apm.Azure.ServiceBus
```

::::{note}
If you are using `Azure.Messaging.ServiceBus` (the current SDK), you can alternatively use [profiler auto-instrumentation](/reference/setup-auto-instrumentation.md) or the [OpenTelemetry Bridge](/reference/opentelemetry-bridge.md) without installing this NuGet package. The legacy `Microsoft.Azure.ServiceBus` package does not emit native OpenTelemetry spans and requires this NuGet package.

When this NuGet package is installed alongside a profiler or OpenTelemetry Bridge setup, the dedicated subscriber takes precedence over the bridge to prevent duplicate spans.
::::

Subscribe the appropriate diagnostics subscriber with the agent. Call this in your application startup, before any Service Bus operations occur:

**If using `Elastic.Apm.NetCoreAll`:**
The subscribers are registered automatically — no further action is required.

**If using `Azure.Messaging.ServiceBus` (current SDK):**

```csharp
using Elastic.Apm;
using Elastic.Apm.Azure.ServiceBus;

Agent.Subscribe(new AzureMessagingServiceBusDiagnosticsSubscriber());
```

**If using `Microsoft.Azure.ServiceBus` (legacy, deprecated):**

```csharp
using Elastic.Apm;
using Elastic.Apm.Azure.ServiceBus;

Agent.Subscribe(new MicrosoftAzureServiceBusDiagnosticsSubscriber());
```

For ASP.NET Core applications, call `Agent.Subscribe()` in your `Program.cs`:

```csharp
using Elastic.Apm;
using Elastic.Apm.Azure.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

Agent.Subscribe(new AzureMessagingServiceBusDiagnosticsSubscriber());

var app = builder.Build();
app.Run();
```

Once instrumented, Service Bus operations appear as transactions and spans in the APM UI.


## Verify your setup [_verify_setup_servicebus]

After configuring the agent and adding the Service Bus instrumentation package:

1. Ensure your application is running and connected to APM Server
2. Send or receive at least one Service Bus message
3. Open Kibana and navigate to **Observability → Applications → Service inventory**
4. Locate your service by name (configured in `ELASTIC_APM_SERVICE_NAME`)
5. Look for transactions and spans related to Service Bus operations

If you don't see data appearing, check:

* The APM Server URL and service name are correctly configured
* Network connectivity between your application and APM Server
* Application logs for any errors or warnings from the Elastic APM agent


## Captured operations [_captured_operations]

Transactions represent the top-level operation, typically an inbound receive or message handler invocation. Spans represent child operations within an existing transaction, such as sending messages.

### Transactions [_transactions]

A new transaction is created when

* a receive operation is initiated against a queue or topic subscription (regardless of whether messages are returned).
* a receive deferred operation (retrieving a previously deferred message by sequence number) is initiated against a queue or topic subscription.
* a message is processed via `ServiceBusProcessor` or `ServiceBusSessionProcessor` — the push-based model where the SDK delivers messages to a registered handler. No additional code is required in your handler.

If a receive or receive deferred operation occurs within an existing transaction, a span is created instead.

### Spans [_spans]

A new span is created when there is a current transaction, and when

* one or more messages are sent to a queue or topic.
* one or more messages are scheduled to a queue or a topic.

### Span links [_span_links]

When receiving or processing a batch of messages, the agent creates a span link for each message back to the producer span that sent it. This preserves the distributed trace across message boundaries and lets you follow each message end-to-end from producer to consumer in the APM UI.


## Configure the agent [_configure_agent_servicebus]

Before Service Bus tracing will work, ensure the agent is connected to your APM Server. See [Minimum configuration](/reference/configuration.md#minimum-configuration) to configure:

* `ELASTIC_APM_SERVER_URL` — APM Server endpoint
* `ELASTIC_APM_SERVICE_NAME` — Your application's name

::::{tip}
For development, you can set these via environment variables. For production, use your application's configuration mechanism (for example, `appsettings.json` for ASP.NET Core).
::::

### Additional messaging options [_additional_messaging_options]

Use [`IgnoreMessageQueues`](/reference/config-messaging.md#config-ignore-message-queues) to exclude specific queues, topics, or subscriptions from being traced. This setting accepts a comma-separated list of wildcard patterns matched against the queue or topic name.

For all other agent configuration options, see [Configuration](/reference/configuration.md).
