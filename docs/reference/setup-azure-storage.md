---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-storage.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Azure Storage [setup-azure-storage]


## Quick start [_quick_start_13]

Instrumentation can be enabled for Azure Storage by referencing [`Elastic.Apm.Azure.Storage`](https://www.nuget.org/packages/Elastic.Apm.Azure.Storage) package and subscribing to diagnostic events using one of the subscribers:

* If the agent is included by referencing the `Elastic.Apm.NetCoreAll` package, the subscribers will be automatically subscribed with the agent, and no further action is required.
* If you’re using `Azure.Storage.Blobs`, subscribe `AzureBlobStorageDiagnosticsSubscriber` with the agent

    ```csharp
    Agent.Subscribe(new AzureBlobStorageDiagnosticsSubscriber());
    ```

* If you’re using `Azure.Storage.Queues`, subscribe `AzureQueueStorageDiagnosticsSubscriber` with the agent

    ```csharp
    Agent.Subscribe(new AzureQueueStorageDiagnosticsSubscriber());
    ```

* If you’re using `Azure.Storage.Files.Shares`, subscribe `AzureFileShareStorageDiagnosticsSubscriber` with the agent

    ```csharp
    Agent.Subscribe(new AzureFileShareStorageDiagnosticsSubscriber());
    ```


For Azure Queue storage,

* A new transaction is created when one or more messages are received from a queue
* A new span is created when there is a current transaction, and when a message is sent to a queue

For Azure Blob storage, a new span is created when there is a current transaction and when a request is made to blob storage.

For Azure File Share storage, a new span is crated when there is a current transaction and when a request is made to file storage.

