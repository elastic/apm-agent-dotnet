# AspNetCoreSampleApp with Elastic .NET Agent. 

The goal of this sample is to show you how you can use the Elastic APM .NET Agent with an ASP.NET Core application.

This app is very similar to the standard default ASP.NET Core MVC template and it is monitored by the Elastic APM agent.

Each HTML page, you see in the browser shows a small explanation of the corresponding sample.

The most important change compared to the standard template can be found in the Startup.cs file:


```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseElasticApm();
````

This activates the Elastic APM middleware which captures incoming HTTP requests plus it also turns on `HttpClient` and Entity Framework Core monitoring.

Additionally, the `Task<IActionResult> ChartPage()` method uses the Public Agent API, which captures the reading of a CSV file as a span.