// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.SqlClient;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.NetCoreAll
{
	public static class HostBuilderExtensions
	{
		/// <summary>
		/// Register Elastic APM .NET Agent with components in the container and enables <see cref="HttpDiagnosticsSubscriber" />,
		/// <see cref="EfCoreDiagnosticsSubscriber" />, and <see cref="SqlClientDiagnosticSubscriber"/>.
		/// </summary>
		/// <param name="builder">Builder.</param>
		public static IHostBuilder UseAllElasticApm(this IHostBuilder builder) => builder.UseElasticApm(new HttpDiagnosticsSubscriber(),
			new AspNetCoreDiagnosticSubscriber(),
			new EfCoreDiagnosticsSubscriber(),
			new SqlClientDiagnosticSubscriber(),
			new ElasticsearchDiagnosticsSubscriber(),
			new GrpcClientDiagnosticSubscriber());
	}
}
