using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Globalization;
using Elastic.Apm;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.AspNetCore
{
    public class ApmMiddleware : IDisposable
    {
        private IPayloadSender payloadSender;
        private readonly RequestDelegate next;

        public ApmMiddleware(RequestDelegate next, IPayloadSender payloadSender = null)
        {
            this.payloadSender = payloadSender ?? new PayloadSender();
            this.next = next;
        }

        public void Dispose()
        {
            if (payloadSender is IDisposable disposablePayloadSender)
            {
                disposablePayloadSender.Dispose();
                disposablePayloadSender = null;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            var transactions = new List<Transaction> {
                    new Transaction {
                        Name = $"{context.Request.Method} {context.Request.Path}",

                        Id = Guid.NewGuid(),
                        Type = "request",
                        TimestampInDateTime = DateTime.UtcNow,
                        Context = new Context
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
                        }
                    }
                };

            TransactionContainer.Transactions.Value = transactions;

            await next(context);

            sw.Stop();

            transactions[0].Duration = sw.ElapsedMilliseconds;
            transactions[0].Result = $"{GetProtocolName(context.Request.Protocol)} {context.Response.StatusCode.ToString()[0]}xx";
            transactions[0].Context.Response = new Response
            {
                Finished = context.Response.HasStarted, //TODO ?
                Status_code = context.Response.StatusCode
            };

            var payload = new Payload
            {
                Service = new Service
                {
                    Agent = new Apm.Model.Payload.Agent
                    {
                        Name = Consts.AgentName,
                        Version = Consts.AgentVersion
                    },
                    Name = Assembly.GetEntryAssembly()?.GetName()?.Name,
                    Framework = new Framework { Name = "ASP.NET Core", Version = "2.1" }, //TODO: Get version
                    Language = new Language { Name = "C#" } //TODO
                },

            };

            payload.Transactions = TransactionContainer.Transactions.Value;
            payloadSender.QueuePayload(payload);
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
