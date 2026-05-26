---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-storage.html
description: "How to enable Elastic APM .NET agent instrumentation of Azure Blob and Queue storage operations using diagnostic event subscribers."
navigation_title: Azure Storage
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up Azure Storage instrumentation [setup-azure-storage]


## Supported versions [_supported_versions_storage]

| Package | Supported versions |
| --- | --- |
| `Azure.Storage.Blobs` | ≥12.8.0 <13.0.0 |
| `Azure.Storage.Queues` | ≥12.6.0 <13.0.0 |
| `Azure.Storage.Files.Shares` | ≥12.6.0 <13.0.0 |

For the full compatibility matrix including supported installation methods, refer to [Azure Storage](/reference/supported-technologies.md#supported-azure-storage).


## Quick start [_quick_start_13]

This page assumes the core agent is already set up. If not, see [Set up the APM .NET agent](/reference/set-up-apm-net-agent.md) first.

Add the [`Elastic.Apm.Azure.Storage`](https://www.nuget.org/packages/Elastic.Apm.Azure.Storage) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.Azure.Storage
```

Subscribe to diagnostic events using the appropriate subscriber for your storage service:

* If the agent is included by referencing the `Elastic.Apm.NetCoreAll` package, the subscribers will be automatically subscribed with the agent, and no further action is required.
* If you’re using `Azure.Storage.Blobs`, subscribe `AzureBlobStorageDiagnosticsSubscriber` with the agent

    ```csharp
    using Elastic.Apm;
    using Elastic.Apm.Azure.Storage;

    Agent.Subscribe(new AzureBlobStorageDiagnosticsSubscriber());
    ```

* If you’re using `Azure.Storage.Queues`, subscribe `AzureQueueStorageDiagnosticsSubscriber` with the agent

    ```csharp
    using Elastic.Apm;
    using Elastic.Apm.Azure.Storage;

    Agent.Subscribe(new AzureQueueStorageDiagnosticsSubscriber());
    ```

* If you’re using `Azure.Storage.Files.Shares`, subscribe `AzureFileShareStorageDiagnosticsSubscriber` with the agent

    ```csharp
    using Elastic.Apm;
    using Elastic.Apm.Azure.Storage;

    Agent.Subscribe(new AzureFileShareStorageDiagnosticsSubscriber());
    ```


For Azure Queue storage,

* A new transaction is created when one or more messages are received from a queue
* A new span is created when there is a current transaction, and when a message is sent to a queue

For Azure Blob storage, a new span is created when there is a current transaction and when a request is made to blob storage.

For Azure File Share storage, a new span is created when there is a current transaction and when a request is made to file storage.

