// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Reflection;
using System.Threading;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;

namespace Elastic.Apm.Benchmarks.AspNetCorePerf
{
	public class AspNetCoreSampleRunner
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		public void StartSampleAppWithAgent(bool withAgent, string url) =>
			SampleAspNetCoreApp.Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
					{
						Startup.ConfigureServicesExceptMvc(services);

						services
							.AddElasticApm(new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber())
							.AddMvc()
							.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))));
					}
				)
				.Configure(app =>
				{
					if (withAgent)
					{
						Environment.SetEnvironmentVariable("ELASTIC_APM_FLUSH_INTERVAL", "0");
					}

					Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls(url)
				.Build()
				.RunAsync(_cancellationTokenSource.Token);
	}
}
