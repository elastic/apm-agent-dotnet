---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-servicebus.html
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

# Azure Service Bus [setup-azure-servicebus]


## Quick start [_quick_start_12]

Instrumentation can be enabled for Azure Service Bus by referencing [`Elastic.Apm.Azure.ServiceBus`](https://www.nuget.org/packages/Elastic.Apm.Azure.ServiceBus) package and subscribing to diagnostic events using one of the subscribers:

1. If the agent is included by referencing the `Elastic.Apm.NetCoreAll` package, the subscribers will be automatically subscribed with the agent, and no further action is required.
2. If you’re using `Microsoft.Azure.ServiceBus`, subscribe `MicrosoftAzureServiceBusDiagnosticsSubscriber` with the agent

    ```csharp
    Agent.Subscribe(new MicrosoftAzureServiceBusDiagnosticsSubscriber());
    ```

3. If you’re using `Azure.Messaging.ServiceBus`, subscribe `AzureMessagingServiceBusDiagnosticsSubscriber` with the agent

    ```csharp
    Agent.Subscribe(new AzureMessagingServiceBusDiagnosticsSubscriber());
    ```


A new transaction is created when

* one or more messages are received from a queue or topic subscription.
* a message is receive deferred from a queue or topic subscription.

A new span is created when there is a current transaction, and when

* one or more messages are sent to a queue or topic.
* one or more messages are scheduled to a queue or a topic.

