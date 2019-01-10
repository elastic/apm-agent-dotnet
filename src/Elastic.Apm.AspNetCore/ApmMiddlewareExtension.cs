using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;
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
		public static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder, IConfiguration configuration = null, IPayloadSender payloadSender = null
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
			var config = configuration != null ? new MicrosoftExtensionsConfig(configuration, service: service) : null;
			Agent.Setup(config);
			return UseElasticApm(builder, Agent.Instance);
		}

		internal static IApplicationBuilder UseElasticApm(this IApplicationBuilder builder, ApmAgent agent)
		{
			System.Diagnostics.DiagnosticListener.AllListeners
				.Subscribe(new DiagnosticInitializer(new[] { new AspNetCoreDiagnosticListener(agent.Config.Logger) }));

			return builder.UseMiddleware<ApmMiddleware>(agent.Tracer);
		}
	}
}
