using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using System.Reflection;
using Elastic.Apm.Model.Payload;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Elastic.Apm.Tests")]
[assembly: InternalsVisibleTo("Elastic.Apm.AspNetCore.Tests")]

namespace Elastic.Apm.AspNetCore
{
    public class ApmMiddleware
    {
        private readonly RequestDelegate next;
        private readonly Service service;

        public ApmMiddleware(RequestDelegate next)
        {
            this.next = next;

            service = new Service
            {
                Agent = new Apm.Model.Payload.Agent
                {
                    Name = Consts.AgentName,
                    Version = Consts.AgentVersion
                },
                Name = Assembly.GetEntryAssembly()?.GetName()?.Name,
                Framework = new Framework { Name = "ASP.NET Core", Version = "2.1" }, //TODO: Get version
                Language = new Language { Name = "C#" } //TODO
            };

            Agent.Api.Service = service;
        }

        public async Task InvokeAsync(HttpContext context)
        {           
            var transaction = Agent.Api.StartTransaction($"{context.Request.Method} {context.Request.Path}",
                                                              Transaction.TYPE_REQUEST);

            transaction.Context = new Context
            {
                Request = new Request
                {
                    Method = context.Request.Method,
                    Socket = new Socket
                    {
                        Encrypted = context.Request.IsHttps,
                        Remote_address = context.Connection?.RemoteIpAddress?.ToString()
                    },
                    Url = new Url
                    {
                        Full = context.Request?.Path.Value,
                        HostName = context.Request.Host.Host,
                        Protocol = GetProtocolName(context.Request.Protocol),
                        Raw = context.Request?.Path.Value //TODO
                    },
                    HttpVersion = GetHttpVersion(context.Request.Protocol)
                }
            };

            await next(context);

            transaction.Result = $"{GetProtocolName(context.Request.Protocol)} {context.Response.StatusCode.ToString()[0]}xx";
            transaction.Context.Response = new Response
            {
                Finished = context.Response.HasStarted, //TODO ?
                Status_code = context.Response.StatusCode
            };

            transaction.End();
        }

        private string GetProtocolName(String protocol)
        {
            switch (protocol)
            {
                case String s when String.IsNullOrEmpty(s):
                    return String.Empty;
                case String s when s.StartsWith("HTTP", StringComparison.InvariantCulture): //in case of HTTP/2.x we only need HTTP
                    return "HTTP";
                default:
                    return protocol;
            }
        }

        private string GetHttpVersion(String protocolString)
        {
            switch (protocolString)
            {
                case "HTTP/1.0":
                    return "1.0";
                case "HTTP/1.1":
                    return "1.1";
                case "HTTP/2.0":
                    return "2.0";
                default:
                    return protocolString.Replace("HTTP/", String.Empty);
            }
        }
    }
}
