## SampleAspNetCoreApp with Elastic .NET Agent. 

This app is the standard default ASP.NET Core MVC template monitored by the Elastic APM agent.

The only change compared to the standard template can be found in the Startup.cs file:


```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseElasticApm();
````

This activates the Elastic APM middleware which captures incoming HTTP requests.