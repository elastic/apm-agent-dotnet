// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.NetCoreAll
{
	public static class ApmMiddlewareExtension
	{
		/// <summary>
		/// Adds the Elastic APM Middleware to the ASP.NET Core pipeline and enables <see cref="HttpDiagnosticsSubscriber" />,
		/// <see cref="EfCoreDiagnosticsSubscriber" />.
		/// This method turns on ASP.NET Core monitoring with every other related monitoring components, for example the agent
		/// will also automatically trace outgoing HTTP requests and database statements.
		/// </summary>
		/// <returns>The elastic apm.</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By
		/// doing this the agent will read agent related configurations through this IConfiguration instance.
		/// If no <see cref="IConfiguration" /> is passed to the agent then it will read configs from environment variables.
		/// </param>
		public static IApplicationBuilder UseAllElasticApm(
			this IApplicationBuilder builder,
			IConfiguration configuration = null
		) => AspNetCore.ApmMiddlewareExtension
			.UseElasticApm(builder, configuration, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber(),
				new SqlClientDiagnosticSubscriber());
	}
}
