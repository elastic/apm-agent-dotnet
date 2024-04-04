using Elastic.Apm.DiagnosticSource;
using WorkerServiceSample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddElasticApm(new HttpDiagnosticsSubscriber()); // register Elastic APM before registering other IHostedServices
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
