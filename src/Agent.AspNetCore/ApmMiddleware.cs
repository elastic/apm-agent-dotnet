using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Elastic.Agent.Core.Report;
using Elastic.Agent.Core.Model.Payload;
using Elastic.Agent.Core;
using System.Reflection;

namespace Elastic.Agent.AspNetCore
{
    public class ApmMiddleware
    {
        private readonly PayloadSender payloadSender = new PayloadSender();
        private readonly RequestDelegate _next;

        public ApmMiddleware(RequestDelegate next)
        {
            _next = next;
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
                                Socket = new Socket{ Encrypted = context.Request.IsHttps, Remote_address = context.Connection.RemoteIpAddress.ToString()},
                                Url = new Url
                                {
                                    Full = context.Request?.Path.Value,
                                    HostName = context.Request.Host.Host,
                                    Protocol = "HTTP", //TODO
                                    Raw = context.Request?.Path.Value
                                }
                                //HttpVersion TODO
                            },
                        },
                    }
                };

            TransactionContainer.Transactions = transactions;

            await _next(context);

            sw.Stop();

            transactions[0].Duration = sw.ElapsedMilliseconds;
            transactions[0].Result = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300 ? "success" : "failed";
            transactions[0].Context.Response = new Response
            {
                Finished = context.Response.HasStarted, //TODO ?
                Status_code = context.Response.StatusCode
            };

            var payload = new Payload
            {
                Service = new Service
                {
                    Agent = new Core.Model.Payload.Agent
                    {
                        Name = Consts.AgentName,
                        Version = Consts.AgentVersion
                    },
                    Name = Assembly.GetEntryAssembly()?.GetName()?.Name,
                    Framework = new Framework { Name = "ASP.NET Core", Version = "2.1" }, //TODO: Get version
                    Language = new Language { Name = "C#" } //TODO
                },

            };

            payload.Transactions = TransactionContainer.Transactions;
            await payloadSender.SendPayload(payload); //TODO: Make it background!
        }
    }
}
