using System;
using System.Collections.Generic;
using System.Text;
using Elastic.Apm.AspNetCore.Config;
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
        /// <param name="configuration">You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By doing this the agent will read agent related configurations through this IConfiguration instance.</param>
        /// <param name="payloadSender">Payload sender.</param>
        public static IApplicationBuilder UseElasticApm(
            this IApplicationBuilder builder, IConfiguration configuration = null, IPayloadSender payloadSender = null)
        {
            if(configuration != null)
            {
                Agent.Config = new MicrosoftExtensionsConfig(configuration);
            }
            return payloadSender == null ? builder.UseMiddleware<ApmMiddleware>() : builder.UseMiddleware<ApmMiddleware>(payloadSender);
        }
    }
}
