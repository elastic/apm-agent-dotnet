// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers Elastic APM .NET Agent into the dependency injection container and enables the <see cref="AspNetCoreDiagnosticSubscriber" />.
	/// You can customize the agent by passing additional <see cref="IDiagnosticsSubscriber"/> components to this method.
	/// </summary>
	/// <param name="services">An <see cref="IServiceCollection"/> where services are to be registered.</param>
	/// <param name="subscribers">Specify zero or more additional diagnostic source subscribers to enable.</param>
	public static IServiceCollection AddElasticApmForAspNetCore(this IServiceCollection services, params IDiagnosticsSubscriber[] subscribers)
	{
		if (subscribers is null || subscribers.Length == 0)
		{
			services.AddElasticApm(new AspNetCoreDiagnosticSubscriber());
		}
		else if (subscribers.Any(s => s is AspNetCoreDiagnosticSubscriber))
		{
			services.AddElasticApm(subscribers);
		}
		else
		{
			var subs = subscribers.ToList();
			subs.Add(new AspNetCoreDiagnosticSubscriber());
			services.AddElasticApm([.. subs]);
		}

		return services;
	}
}
