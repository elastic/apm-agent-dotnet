using System;
using System.Collections.Generic;
using System.Text;
using Elastic.Agent.Core.Report;
using Microsoft.AspNetCore.Builder;

namespace Elastic.Agent.AspNetCore
{
    public static class ApmMiddlewareExtension
    {
        public static IApplicationBuilder UseElasticApm(
            this IApplicationBuilder builder, IPayloadSender payloadSender = null)
        {
            return payloadSender == null ? builder.UseMiddleware<ApmMiddleware>() : builder.UseMiddleware<ApmMiddleware>(payloadSender);
        }
    }
}
