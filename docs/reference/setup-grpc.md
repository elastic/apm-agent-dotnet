---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-grpc.html
description: "How to enable Elastic APM .NET Agent automatic instrumentation of gRPC client and server-side calls."
navigation_title: gRPC
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up gRPC instrumentation [setup-grpc]


## Supported versions [_supported_versions_grpc]

| Package | Supported versions |
| --- | --- |
| `Grpc.Net.Client` | ≥2.23.2 <3.0.0 |

gRPC server-side instrumentation is built in to ASP.NET Core (≥8.0.0 ≤10.0.x) and does not require an additional package.

For the full compatibility matrix including supported installation methods, refer to [RPC frameworks](/reference/supported-technologies.md#supported-rpc-frameworks).


## Quick start [_quick_start_8]

This page assumes the core agent is already set up. If not, see [Set up the {{product.apm-agent-dotnet}}](/reference/set-up-apm-net-agent.md) first.

### Server-side

Server-side gRPC instrumentation is automatically included when using the [ASP.NET Core setup](/reference/setup-asp-net-core.md). No additional package is required.

### Client-side

Add the [`Elastic.Apm.GrpcClient`](https://www.nuget.org/packages/Elastic.Apm.GrpcClient) NuGet package to your project:

```sh
dotnet add package Elastic.Apm.GrpcClient
```

Pass `GrpcClientDiagnosticListener` to the `AddElasticApm` method in case of ASP.NET Core:

```csharp
using Elastic.Apm.GrpcClient;

app.Services.AddElasticApm(new GrpcClientDiagnosticListener());
```

or passing `GrpcClientDiagnosticSubscriber` to the `Subscribe` method

```csharp
using Elastic.Apm;
using Elastic.Apm.GrpcClient;

Agent.Subscribe(new GrpcClientDiagnosticSubscriber());
```

Diagnostic events from `Grpc.Net.Client` are captured as spans.
