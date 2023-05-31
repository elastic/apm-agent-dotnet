# WebApiSample with Elastic .NET Agent. 

This is a very small ASP.NET Core WebApi application with the Elastic APM .NET Agent. This sample is intended to showcase the distributed tracing capabilities of the agent.

When you start the application it starts listening on `http://localhost:5050`, and under `http://localhost:5050/api/values` there is a controller that does the following: 
- returns a list of strings as JSON 
- all strings are static except the last one
- it creates an HTTP GET request to `https://elastic.co`
- the last string is the result of the http request.

The `SampleAspNetCoreApp` sample under `/Home/DistributedTracingMiniSample` creates an HTTP call to `http://localhost:5050/api/values`. As the result the Elastic APM will capture a trace including the 2 ASP.NET Core applications. You can look at this trace in Kibana, where you will see that the 2 services were correlated into a single trace.
