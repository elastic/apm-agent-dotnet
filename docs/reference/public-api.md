---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/public-api.html
---

# Public API [public-api]

The public API of the Elastic APM .NET agent lets you customize and manually create spans and transactions, as well as track errors.


## Initialization [api-initialization]

The API does not require explicit Agent initialization—agent initialization is optional. The `Elastic.Apm.Agent.IsConfigured` property lets you check whether the agent is already initialized.


### Implicit agent initialization [implicit-initialization]

If you don’t explicitly initialize the agent, it will be started with a default component setup. This means the Agent will read [configuration settings](/reference/configuration.md) from environment variables. If you don’t set an environment variable, the Agent will use the default value. For example, the `ServerUrl` default is `http://localhost:8200`.

This implicit initialization of the agent happens on the first call on the `Elastic.Apm.Agent` class.

::::{note}
One exception is the `Elastic.Apm.Agent.IsConfigured` method. This method never initializes the agent, it only checks if the agent is already initialized.
::::


Another example of initialization is when you enable the Agent with one of the technology-specific methods from the [*Set up the Agent*](/reference/set-up-apm-net-agent.md) instructions. Specifically when the `AddElasticApm` or `AddAllElasticApm` method is called in ASP.NET Core or when the IIS module is initialized in an IIS application.

The default agent setup should cover most of the use cases and the primary way to configure the agent is through environment variables.


### Explicit agent initialization [explicit-initialization]

If you would like to replace one of the agent components, you can do so by calling the `Elastic.Apm.Agent.Setup(AgentComponents)` method. In the AgentComponents you can pass following optional components to the agent:

* `IApmLogger`: A logger implementation that will be used to print log messages. Default: A console logger.
* `IPayloadSender`: A component that receives all the captured events like spans, transactions, and metrics. The default implementation serializes all events and sends them to the Elastic APM Server
* `IConfigurationReader`: A component that reads [agent configuration settings](/reference/configuration.md). The default implementation reads configuration through environment variables.

::::{note}
In the case of ASP.NET Core, when you register the agent, the `AddElasticApm` and the `AddAllElasticApm` methods both implicitly initialize the agent by calling the `Elastic.Apm.Agent.Setup` method internally. In that setup, the `IConfigurationReader` implementation will read configuration from the ASP.NET Core configuration system in case you pass an `IConfiguration` instance to the method. The `IApmLogger` instance will also log through the configured logging provider by integrating into the ASP.NET Core logging system.
::::



## Auto instrumentation in combination with the Public Agent API [auto-instrumentation-and-agent-api]

With the `Elastic.Apm.Agent.Subscribe(params IDiagnosticsSubscriber[] subscribers)` method you can turn on auto instrumentation for supported libraries.

In the case of ASP.NET Core, when you turn on the agent with the `AddAllElasticApm` method, the agent will do this automatically.

With a typical console application, you need to do this manually by calling `Elastic.Apm.Agent.Subscribe(params IDiagnosticsSubscriber[] subscribers)` method somewhere in your application, ideally in the startup code.

`IDiagnosticsSubscriber` implementations are offered by the agent and they subscribe to diagnostic source events or other data sources in order to capture events automatically.

Some examples:

* $$$setup-http$$$`HttpDiagnosticsSubscriber`: captures HTTP calls through `HttpClient` and `HttpWebRequest`

    ```csharp
    Agent.Subscribe(new HttpDiagnosticsSubscriber());
    ```

* `EfCoreDiagnosticsSubscriber`: captures database calls through Entity Framework Core
* `SqlClientDiagnosticSubscriber`: captures database calls through `SqlClient`

::::{note}
When the agent is configured with [`Enabled` set to `false`](/reference/config-core.md#config-enabled), `Elastic.ApmAgent.Subscribe(params IDiagnosticsSubscriber[] subscribers)` will not subscribe the subscribers to diagnostic source events.

::::



## Tracer API [api-tracer-api]

The tracer gives you access to the currently active transaction and it enables you to manually start a transaction.

You can access the API by using the static property on the Agent: `Elastic.Apm.Agent.Tracer`.


#### `ITransaction StartTransaction(string name, string type, DistributedTracingData = null)` [api-start-transaction]

Use this method to create a custom transaction.

Note that in the case of auto-instrumentation, the agent will automatically do this for you. For example, if you have incoming HTTP calls in an ASP.NET Core application, the agent automatically starts a transaction. In these instances, this method is not needed.

It’s important to call [`void End()`](#api-transaction-end) when the transaction has ended. A best practice is to use the transaction in a try-catch-finally block or to use the [`CaptureTransaction`](#convenient-capture-transaction) method.

Example:

```csharp
var transaction = Elastic.Apm.Agent
        .Tracer.StartTransaction("MyTransaction", ApiConstants.TypeRequest);
try
{
    //application code that is captured as a transaction
}
catch (Exception e)
{
    transaction.CaptureException(e);
    throw;
}
finally
{
    transaction.End();
}
```


#### `ITransaction CurrentTransaction` [api-current-transaction]

Returns the currently active transaction. See the [Transaction API](#api-transaction) to customize the current transaction.

If there is no current transaction, this method will return `null`.

```csharp
var transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;
```


#### `ISpan CurrentSpan` [api-current-span]

Returns the currently active span. See the [Span API](#api-span) to customize the current span.

If there is no current span, this method will return `null`.

```csharp
var span = Elastic.Apm.Agent.Tracer.CurrentSpan;
```


#### `CaptureTransaction` [convenient-capture-transaction]

This is a convenient method which starts and ends a transaction and captures unhandled exceptions. It has 3 required parameters:

* `name`: The name of the transaction
* `type` The type of the transaction
* One of the following types which references the code that you want to capture as a transaction:

    * `Action`
    * `Action<ITransaction>`
    * `Func<T>`
    * `Func<ITransaction,T>`
    * `Func<Task>`
    * `Func<ITransaction,Task>`
    * `Func<Task<T>>`
    * `Func<ITransaction,Task<T>>`


And an optional parameter:

* `distributedTracingData`: A `DistributedTracingData` instance that contains the distributed tracing information in case you want the new transaction to be a part of a trace.

The following code is the equivalent of the previous example with the convenient API. It automatically starts and ends the transaction and reports unhandled exceptions. The `t` parameter gives you access to the `ITransaction` instance which represents the transaction that you just created.

```csharp
Elastic.Apm.Agent.Tracer
        .CaptureTransaction("TestTransaction", ApiConstants.TypeRequest, (t) =>
{
   //application code that is captured as a transaction
});
```

This API also supports `async` methods with the `Func<Task>` overloads.

::::{note}
The duration of the transaction will be the timespan between the first and the last line of the `async` lambda expression.
::::


Example:

```csharp
await Elastic.Apm.Agent.Tracer
        .CaptureTransaction("TestTransaction", "TestType", async () =>
{
    //application code that is captured as a transaction
    await Task.Delay(500); //sample async code
});
```

::::{note}
Return value of [`CaptureTransaction`](#convenient-capture-transaction) method overloads that accept Task (or Task<T>) is the same Task (or Task<T>) instance as the one passed as the argument so if your application should continue only after the task completes you have to call [`CaptureTransaction`](#convenient-capture-transaction) with `await` keyword.
::::



#### Manually propagating distributed tracing context [manually-propagating-distributed-tracing-context]

Agent automatically propagates distributed tracing context for the supported technologies (see [Networking client-side technologies](/reference/supported-technologies.md#supported-networking-client-side-technologies)). If your application communicates over a protocol that is not supported by the agent you can manually propagate distributed tracing context from the caller to the callee side using Public Agent API.

First you serialize distributed tracing context on the caller side:

```csharp
string outgoingDistributedTracingData =
    (Agent.Tracer.CurrentSpan?.OutgoingDistributedTracingData
        ?? Agent.Tracer.CurrentTransaction?.OutgoingDistributedTracingData)?.SerializeToString();
```

Then you transfer the resulted string to the callee side and you continue the trace by passing deserialized distributed tracing context to any of [`ITransaction StartTransaction(string name, string type, DistributedTracingData = null)`](#api-start-transaction) or [`CaptureTransaction`](#convenient-capture-transaction) APIs - all of these APIs have an optional `DistributedTracingData` parameter. For example:

```csharp
var transaction2 = Agent.Tracer.StartTransaction("Transaction2", "TestTransaction",
     DistributedTracingData.TryDeserializeFromString(serializedDistributedTracingData));
```

::::{note}
The `OutgoingDistributedTracingData` property can be `null`. One such scenario is when the agent is disabled.
::::



#### `void CaptureError(string message, string culprit, StackFrame[] frames = null, string parentId = null);` [api-start-capture-error]

Use this method to capture an APM error with a message and a culprit.

::::{note}
Captured errors are automatically correlated with the active transaction. If no transaction is active, the error will still appear in the APM app but will not be correlated with a transaction.
::::


Example:

```csharp
Agent.Tracer.CaptureError("Something went wrong", "Database issue");
```


#### `void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null);` [api-start-capture-exception]

Use this method to capture a .NET exception as an APM error.

::::{note}
Captured errors are automatically correlated with the active transaction. If no transaction is active, the error will still appear in the APM app but will not be correlated with a transaction.
::::


Example:

```csharp
try
{
	//run my code
}
catch (Exception e)
{
	Agent.Tracer.CaptureException(e);
	//handle error
}
```


#### `void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null);` [api-start-capture-error-log]

Use this method to capture a log event as an APM error.

::::{note}
Captured errors are automatically correlated with the active transaction. If no transaction is active, the error will still appear in the APM app but will not be correlated with a transaction.
::::


Example:

```csharp
var errorLog = new ErrorLog("Error message")
{
	Level = "error",
	ParamMessage = "42"
};

Agent.Tracer.CaptureErrorLog(errorLog);
```


## Transaction API [api-transaction]

A transaction describes an event captured by an Elastic APM agent monitoring a service. Transactions help combine multiple [Spans](#api-span) into logical groups, and they are the first [Span](#api-span) of a service. More information on Transactions and Spans is available in the [APM data model](docs-content://solutions/observability/apps/learn-about-application-data-types.md) documentation.

See [`ITransaction CurrentTransaction`](#api-current-transaction) on how to get a reference of the current transaction.

::::{note}
Calling any of the transaction’s methods after [`void End()`](#api-transaction-end) has been called is illegal. You may only interact with a transaction when you have control over its lifecycle.
::::



#### `ISpan StartSpan(string name, string type, string subType = null, string action = null)` [api-transaction-create-span]

Start and return a new custom span as a child of the given transaction.

It is important to call [`void End()`](#api-span-end) when the span has ended or to use the [`CaptureSpan`](#convenient-capture-span) method. A best practice is to use the span in a try-catch-finally block.

Example:

```csharp
ISpan span = transaction.StartSpan("Select FROM customer",
     ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
try
{
    //execute db query
}
catch(Exception e)
{
    span.CaptureException(e);
    throw;
}
finally
{
    span.End();
}
```


#### `void SetLabel(string key, T value)` [1.7.0] [api-transaction-set-label]

Labels are used to add **indexed** information to transactions, spans, and errors. Indexed means the data is searchable and aggregatable in Elasticsearch. Multiple labels can be defined with different key-value pairs.

* Indexed: Yes
* Elasticsearch type: [object](elasticsearch://docs/reference/elasticsearch/mapping-reference/object.md)
* Elasticsearch field: `labels` (previously `context.tags` in <v.7.0)

Label values can be a string, boolean, or number. Because labels for a given key are stored in the same place in Elasticsearch, all label values of a given key must have the same data type. Multiple data types per key will throw an exception, e.g., `{"foo": "bar"}` and `{"foo": 42}`.

::::{note}
Number and boolean labels were only introduced in APM Server 6.7+. Using this API in combination with an older APM Server versions leads to validation errors.
::::


::::{important}
Avoid defining too many user-specified labels. Defining too many unique fields in an index is a condition that can lead to a [mapping explosion](docs-content://manage-data/data-store/mapping.md#mapping-limit-settings).
::::


```csharp
transaction.SetLabel("stringSample", "bar");
transaction.SetLabel("boolSample", true);
transaction.SetLabel("intSample", 42);
```

* `String key`:   The tag key
* `String|Number|bool value`: The tag value


#### `T TryGetLabel<T>(string key, out T value)` [1.7.1] [api-transaction-try-get-label]

Returns the transaction’s label in the `value` out parameter. If the `key` does not exist, this method returns false. Labels can be added with the [SetLabel](#api-transaction-set-label) method.

```csharp
if(transaction.TryGetLabel<int>("foo", our var myLabel))
    Console.WriteLine(myLabel);
```


#### `Dictionary<string,string> Labels` [api-transaction-tags]

::::{warning}
This property is obsolete and will be be removed in a future version. Use the [`void SetLabel()`](#api-transaction-set-label) method instead, which allows setting labels of string, boolean and number. This property remains for now in order to not break binary compatibility, and at serialization time, the values set with `.SetLabel()` are combined with `Labels` to form the set of labels sent to APM server, with values in `Labels` taking precedence.
::::


A flat mapping of user-defined labels with string values.

If the key contains any special characters (`.`,`*`, `"`), they will be replaced with underscores. For example `a.b` will be stored as `a_b`.

::::{tip}
Before using custom labels, ensure you understand the different types of [metadata](docs-content://solutions/observability/apps/metadata.md) that are available.
::::


::::{warning}
Avoid defining too many user-specified labels. Defining too many unique fields in an index is a condition that can lead to a [mapping explosion](docs-content://manage-data/data-store/mapping.md#mapping-limit-settings).
::::


```csharp
Agent.Tracer
 .CaptureTransaction(TransactionName, TransactionType,
    transaction =>
    {
        transaction.Labels["foo"] = "bar";
        //application code that is captured as a transaction
    });
```

* `key`:   The label key
* `value`: The label value


#### `void End()` [api-transaction-end]

Ends the transaction and schedules it to be reported to the APM Server.

It is illegal to call any methods on a span instance which has already ended. This also includes this method and [`ISpan StartSpan(string name, string type, string subType = null, string action = null)`](#api-transaction-create-span).

Example:

```csharp
transaction.End();
```

::::{note}
If you use the [`CaptureTransaction`](#convenient-capture-transaction) method you must not call [`void End()`](#api-transaction-end).
::::



#### `void CaptureException(Exception e)` [api-transaction-capture-exception]

Captures an exception and reports it to the APM server.


#### `void CaptureError(string message, string culprit, StackFrame[] frames)` [api-transaction-capture-error]

Captures a custom error and reports it to the APM server.

This method is typically used when you want to report an error, but you don’t have an `Exception` instance.


#### `void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null);` [api-transaction-capture-error-log]

Captures a custom error and reports it to the APM server with a log attached to it.

This method is typically used when you already log errors in your code and you want to attach this error to an APM transaction. The log will show up on the APM UI as part of the error and it will be correlated to the transaction.


#### `CaptureSpan` [convenient-capture-span]

This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions. It has the same overloads as the [`CaptureTransaction`](#convenient-capture-transaction) method.

It has 3 required parameters:

* `name`: The name of the span
* `type` The type of the span
* One of the following types which references the code that you want to capture as a transaction:

    * `Action`
    * `Action<ITransaction>`
    * `Func<T>`
    * `Func<ITransaction,T>`
    * `Func<Task>`
    * `Func<ITransaction,Task>`
    * `Func<Task<T>>`
    * `Func<ITransaction,Task<T>>`


and 2 optional parameters:

* `supType`: The subtype of the span
* `action`: The action of the span

The following code is the equivalent of the previous example from the [`ISpan StartSpan(string name, string type, string subType = null, string action = null)`](#api-transaction-create-span) section with the convenient API. It automatically starts and ends the span and reports unhandled exceptions. The `s` parameter gives you access to the `ISpan` instance which represents the span that you just created.

```csharp
ITransaction transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;

transaction.CaptureSpan("SampleSpan", ApiConstants.TypeDb, (s) =>
{
    //execute db query
}, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
```

Similar to the [`CaptureTransaction`](#convenient-capture-transaction) API, this method also supports `async` methods with the `Func<Task>` overloads.

::::{note}
The duration of the span will be the timespan between the first and the last line of the `async` lambda expression.
::::


This example shows you how to track an `async` code block that returns a result (`Task<T>`) as a span:

```csharp
ITransaction transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;
var asyncResult = await transaction.CaptureSpan("Select FROM customer", ApiConstants.TypeDb, async(s) =>
{
    //application code that is captured as a span
    await Task.Delay(500); //sample async code
    return 42;
});
```

::::{note}
Return value of [`CaptureSpan`](#convenient-capture-span) method overloads that accept Task (or Task<T>) is the same Task (or Task<T>) instance as the one passed as the argument so if your application should continue only after the task completes you have to call [`CaptureSpan`](#convenient-capture-span) with `await` keyword.
::::


::::{note}
Code samples above use `Elastic.Apm.Agent.Tracer.CurrentTransaction`. In production code you should make sure the `CurrentTransaction` is not `null`.
::::



#### `EnsureParentId` [api-transaction-ensure-parent-id]

If the transaction does not have a ParentId yet, calling this method generates a new ID, sets it as the ParentId of this transaction, and returns it as a `string`.

This enables the correlation of the spans the JavaScript Real User Monitoring (RUM) agent creates for the initial page load with the transaction of the backend service.

If your service generates the HTML page dynamically, initializing the JavaScript RUM agent with the value of this method allows analyzing the time spent in the browser vs in the backend services.

To enable the JavaScript RUM agent in ASP.NET Core, initialize the RUM agent with the .NET agent’s current transaction:

```JavaScript
<script>
	elasticApm.init({
		serviceName: 'MyService',
		serverUrl: 'http://localhost:8200',
		pageLoadTraceId: '@Elastic.Apm.Agent.Tracer.CurrentTransaction?.TraceId',
		pageLoadSpanId: '@Elastic.Apm.Agent.Tracer.CurrentTransaction?.EnsureParentId()',
		pageLoadSampled: @Json.Serialize(Elastic.Apm.Agent.Tracer?.CurrentTransaction.IsSampled)
		})
</script>
```

See the  [JavaScript RUM agent documentation](apm-agent-rum-js://docs/reference/index.md) for more information.


#### `Dictionary<string,string> Custom` [api-transaction-custom]

Custom context is used to add non-indexed, custom contextual information to transactions. Non-indexed means the data is not searchable or aggregatable in Elasticsearch, and you cannot build dashboards on top of the data. However, non-indexed information is useful for other reasons, like providing contextual information to help you quickly debug performance issues or errors.

If the key contains any special characters (`.`,`*`, `"`), they will be replaced with underscores. For example `a.b` will be stored as `a_b`.

Unlike [`Dictionary<string,string> Labels`](#api-transaction-tags), the data in this property is not trimmed.

```csharp
Agent.Tracer.CaptureTransaction(transactionName, transactionType, (transaction) =>
{
	transaction.Custom["foo"] = "bar";
	transaction.End();
});
```


#### `void SetService(string serviceName, string serviceVersion)` ([1.7]) [api-transaction-set-service]

Overwrite the service name and version on a per transaction basis. This is useful when you host multiple services in a single process.

When not set, transactions are associated with the default service.

This method has two `string` parameters:

* `serviceName`: The name of the service to associate with the transaction.
* `serviceVersion`: The version of the service to associate with the transaction.

Usage:

```csharp
var transaction = agent.Tracer.StartTransaction("Transaction1", "sample");
transaction.SetService("MyServiceName", "1.0-beta1");
```

It can also be used with the [Filter API ([1.5])](#filter-api):

```csharp
Agent.AddFilter( transaction =>
{
	transaction.SetService("MyServiceName", "1.0-beta1");
	return transaction;
});
```


#### `Context` [api-transaction-context]

You can attach additional context to manually captured transactions.

If you use a web framework for which agent doesn’t capture transactions automatically (see [Web frameworks](/reference/supported-technologies.md#supported-web-frameworks)), you can add context related to the captured transaction by setting various properties of transaction’s `Context` property. For example:

```csharp
Agent.Tracer.CaptureTransaction("MyCustomTransaction",ApiConstants.TypeRequest, (transaction) =>
{
  transaction.Context.Request = new Request(myRequestMethod, myRequestUri);

  // ... code executing the request

  transaction.Context.Response =
     new Response { StatusCode = myStatusCode, Finished = wasFinished };
});
```


## Span API [api-span]

A span contains information about a specific code path, executed as part of a transaction.

If for example a database query happens within a recorded transaction, a span representing this database query may be created. In such a case, the name of the span will contain information about the query itself, and the type will hold information about the database type.


#### `ISpan StartSpan(string name, string type, string subType = null, string action = null)` [api-span-create-span]

Start and return a new custom span as a child of the given span. Very similar to the [`ISpan StartSpan(string name, string type, string subType = null, string action = null)`](#api-transaction-create-span) method on `ITransaction`, but in this case the parent of the newly created span is a span itself.

It is important to call [`void End()`](#api-span-end) when the span has ended or to use the [`CaptureSpan`](#convenient-capture-span) method. A best practice is to use the span in a try-catch-finally block.

Example:

```csharp
ISpan childSpan = parentSpan.StartSpan("Select FROM customer",
     ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
try
{
    //execute db query
}
catch(Exception e)
{
    childSpan?.CaptureException(e);
    throw;
}
finally
{
    childSpan?.End();
}
```


#### `void SetLabel(string key, T value)` [1.7.0] [api-span-set-label]

A flat mapping of user-defined labels with string, number or boolean values.

::::{note}
In version 6.x, labels are stored under `context.tags` in Elasticsearch. As of version 7.x, they are stored as `labels` to comply with the [Elastic Common Schema (ECS)](https://github.com/elastic/ecs).
::::


::::{note}
The labels are indexed in Elasticsearch so that they are searchable and aggregatable. By all means, you should avoid that user specified data, like URL parameters, is used as a tag key as it can lead to mapping explosions.
::::


```csharp
span.SetLabel("stringSample", "bar");
span.SetLabel("boolSample", true);
span.SetLabel("intSample", 42);
```

* `String key`:   The tag key
* `String|Number|bool value`: The tag value


#### `T TryGetLabel<T>(string key, out T value)` [1.7.1] [api-span-try-get-label]

Returns the span’s label in the `value` out parameter. If the `key` does not exist, this method returns false. Labels can be added with the [SetLabel](#api-span-set-label) method.

```csharp
if(span.TryGetLabel<bool>("foo", out var myLabel))
    Console.WriteLine(myLabel);
```


#### `Dictionary<string,string> Labels` [api-span-tags]

::::{warning}
This property is obsolete and will be be removed in a future version. Use the [`void SetLabel()`](#api-span-set-label) method instead, which allows setting labels of string, boolean and number. This property remains for now in order to not break binary compatibility, and at serialization time, the values set with `.SetLabel()` are combined with `Labels` to form the set of labels sent to APM server, with values in `Labels` taking precedence.
::::


Similar to [`Dictionary<string,string> Labels`](#api-transaction-tags) on the [Transaction API](#api-transaction): A flat mapping of user-defined labels with string values.

If the key contains any special characters (`.`,`*`, `"`), they will be replaced with underscores. For example `a.b` will be stored as `a_b`.

::::{tip}
Before using custom labels, ensure you understand the different types of [metadata](docs-content://solutions/observability/apps/metadata.md) that are available.
::::


::::{warning}
Avoid defining too many user-specified labels. Defining too many unique fields in an index is a condition that can lead to a [mapping explosion](docs-content://manage-data/data-store/mapping.md#mapping-limit-settings).
::::


```csharp
transaction.CaptureSpan(SpanName, SpanType,
span =>
    {
        span.Labels["foo"] = "bar";
        //application code that is captured as a span
    });
```


#### `void CaptureException(Exception e)` [api-span-capture-exception]

Captures an exception and reports it to the APM server.


#### `void CaptureError(string message, string culprit, StackFrame[] frames)` [api-span-capture-error]

Captures a custom error and reports it to the APM server.

This method is typically used when you want to report an error, but you don’t have an `Exception` instance.


#### `void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null);` [api-span-capture-error-log]

Captures a custom error and reports it to the APM server with a log attached to it.

This method is typically used when you already log errors in your code and you want to attach this error to an APM transaction. The log will show up on the APM UI as part of the error and it will be correlated to the transaction of the given span.


#### `void End()` [api-span-end]

Ends the span and schedules it to be reported to the APM Server.

It is illegal to call any methods on a span instance which has already ended.


#### `Context` [api-span-context]

You can attach additional context to manually captured spans.

If you use a database library for which agent doesn’t capture spans automatically (see [Data access technologies](/reference/supported-technologies.md#supported-data-access-technologies)), you can add context related to the captured database operation by setting span’s `Context.Db` property. For example:

```csharp
Agent.Tracer.CurrentTransaction.CaptureSpan("MyDbWrite", ApiConstants.TypeDb, (span) =>
{
    span.Context.Db = new Database
        { Statement = myDbStatement, Type = myDbType, Instance = myDbInstance };

    // ... code executing the database operation
});
```

If you use an HTTP library for which agent doesn’t capture spans automatically (see [Networking client-side technologies](/reference/supported-technologies.md#supported-networking-client-side-technologies)), you can add context related to the captured HTTP operation by setting span’s `Context.Http` property. For example:

```csharp
Agent.Tracer.CurrentTransaction.CaptureSpan("MyHttpOperation", ApiConstants.TypeExternal, (span) =>
{
    span.Context.Http = new Http
        { Url = myUrl, Method = myMethod };

    // ... code executing the HTTP operation

    span.Context.Http.StatusCode = myStatusCode;
});
```


#### `CaptureSpan` [convenient-span-capture-span]

This is a convenient method which starts and ends a child span on the given span and captures unhandled exceptions.

Very similar to the [`CaptureSpan`](#convenient-capture-span) method on `ITransaction`, but in this case the parent of the newly created span is a span itself.

It has 3 required parameters:

* `name`: The name of the span
* `type` The type of the span
* One of the following types which references the code that you want to capture as a transaction:

    * `Action`
    * `Action<ITransaction>`
    * `Func<T>`
    * `Func<ITransaction,T>`
    * `Func<Task>`
    * `Func<ITransaction,Task>`
    * `Func<Task<T>>`
    * `Func<ITransaction,Task<T>>`


and 2 optional parameters:

* `supType`: The subtype of the span
* `action`: The action of the span

The following code is the equivalent of the previous example from the [`ISpan StartSpan(string name, string type, string subType = null, string action = null)`](#api-span-create-span) section with the convenient API. It automatically starts and ends the span and reports unhandled exceptions. The `s` parameter gives you access to the `ISpan` instance which represents the span that you just created.

```csharp
span.CaptureSpan("SampleSpan", ApiConstants.TypeDb, (s) =>
{
    //execute db query
}, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
```

Similar to the [`CaptureTransaction`](#convenient-capture-transaction) API, this method also supports `async` methods with the `Func<Task>` overloads.

::::{note}
The duration of the span will be the timespan between the first and the last line of the `async` lambda expression.
::::


This example shows you how to track an `async` code block that returns a result (`Task<T>`) as a span:

```csharp
var asyncResult = await span.CaptureSpan("Select FROM customer", ApiConstants.TypeDb, async(s) =>
{
    //application code that is captured as a span
    await Task.Delay(500); //sample async code
    return 42;
});
```

::::{note}
Return value of [`CaptureSpan`](#convenient-capture-span) method overloads that accept Task (or Task<T>) is the same Task (or Task<T>) instance as the one passed as the argument so if your application should continue only after the task completes you have to call [`CaptureSpan`](#convenient-capture-span) with `await` keyword.
::::


::::{note}
Code samples above use `Elastic.Apm.Agent.Tracer.CurrentTransaction`. In production code you should make sure the `CurrentTransaction` is not `null`.
::::



## Filter API ([1.5]) [filter-api]

Use `Agent.AddFilter(filter)` to supply a filter callback.

Each filter callback will be called just before data is sent to the APM Server. This allows you to manipulate the data being sent, like to remove sensitive information such as passwords.

Each filter callback is called in the order they are added and will receive a payload object containing the data about to be sent to the APM Server as the only argument.

The filter callback is synchronous and should return the manipulated payload object. If a filter callback doesn’t return any value or returns a falsy value, the remaining filter callback will not be called and the payload will not be sent to the APM Server.

There are 3 overloads of the `Agent.AddFilter` method with the following arguments:

* `Func<ITransaction, ITransaction>`: A filter called for every transaction.
* `Func<ISpan, ISpan>`: A filter called for every span.
* `Func<IError, IError>`: A filter called for every error.

Below are some usage examples of the Agent.AddFilter method.

Drop all spans for a specific database:

```csharp
Agent.AddFilter((ISpan span) =>
{
	if (span.Context?.Db?.Instance == "VerySecretDb")
		return null;
	return span;
});
```

Hide some data:

```csharp
Agent.AddFilter((ITransaction transaction) =>
{
	transaction.Context.Request.Url.Protocol = "[HIDDEN]";
	return transaction;
});
```

