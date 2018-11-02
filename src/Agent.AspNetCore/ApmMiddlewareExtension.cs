using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace Elastic.Agent.AspNetCore
{
	public static class ApmMiddlewareExtension
	{
		public static IApplicationBuilder UseElasticApm(
		this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<ApmMiddleware>();
		}
	}
}
