---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-grpc.html
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

# gRPC [setup-grpc]


## Quick start [_quick_start_8]

Automatic instrumentation for gRPC can be enabled for both client-side and server-side gRPC calls.

Automatic instrumentation for ASP.NET Core server-side is built in to [NuGet package](/reference/setup-asp-net-core.md)

Automatic instrumentation can be enabled for the client-side by referencing [`Elastic.Apm.GrpcClient`](https://www.nuget.org/packages/Elastic.Apm.GrpcClient) package and passing `GrpcClientDiagnosticListener` to the `AddElasticApm` method in case of ASP.NET Core

```csharp
app.Services.AddElasticApm(new GrpcClientDiagnosticListener());
```

or passing `GrpcClientDiagnosticSubscriber` to the `Subscribe` method

```csharp
Agent.Subscribe(new GrpcClientDiagnosticSubscriber());
```

Diagnostic events from `Grpc.Net.Client` are captured as spans.

