using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace Apm_Agent_DotNet.AspNetCore
{
	public static class ApmMiddlewareExtension
	{
		public static IApplicationBuilder UseApm(
		this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<ApmMiddleware>();
		}
	}
}
