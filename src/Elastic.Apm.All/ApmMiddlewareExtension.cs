using System;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.All
{
	public static class ApmMiddlewareExtension
	{
		/// <summary>
		/// Adds the Elastic APM Middleware to the ASP.NET Core pipeline and enables <see cref="HttpDiagnosticsSubscriber"/>, <see cref="EfCoreDiagnosticsSubscriber"/>.
		/// </summary>
		/// <returns>The elastic apm.</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By
		/// doing this the agent will read agent related configurations through this IConfiguration instance.
		/// If no <see cref="IConfiguration"/> is passed to the agent then it will read configs from environment variables.
		/// </param>
		public static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			IConfiguration configuration = null
		)
		{
			return AspNetCore.ApmMiddlewareExtension
				.UseElasticApm(builder, configuration, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
		}
	}
}
