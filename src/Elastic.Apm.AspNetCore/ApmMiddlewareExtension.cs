using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model.Payload;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore
{
	public static class ApmMiddlewareExtension
	{
		/// <summary>
		/// Adds the Elastic APM Middleware to the ASP.NET Core pipeline
		/// </summary>
		/// <returns>The elastic apm.</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By
		/// doing this the agent will read agent related configurations through this IConfiguration instance.
		/// </param>
		/// <param name="payloadSender">Payload sender.</param>
		/// <param name="subscribers">Specify which diagnostic source subscribers you want to connect</param>
		public static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			IConfiguration configuration = null,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			var service = new Service
			{
				Agent = new Service.AgentC
				{
					Name = Consts.AgentName,
					Version = Consts.AgentVersion
				},
				Name = Assembly.GetEntryAssembly()?.GetName().Name,
				Framework = new Framework { Name = "ASP.NET Core", Version = "2.1" }, //TODO: Get version
				Language = new Language { Name = "C#" } //TODO
			};
			var configReader = configuration != null ? new MicrosoftExtensionsConfig(configuration) : null;
			var config = new AgentComponents(configurationReader: configReader, service: service);
			Agent.Setup(config);
			return UseElasticApm(builder, Agent.Instance, subscribers);
		}

		internal static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			ApmAgent agent,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			var subs = new List<IDiagnosticsSubscriber>(subscribers ?? Array.Empty<IDiagnosticsSubscriber>());
			subs.Add(new AspNetCoreDiagnosticsSubscriber());
			agent.Subscribe(subs.ToArray());
			return builder.UseMiddleware<ApmMiddleware>(agent.Tracer);
		}
	}
}
