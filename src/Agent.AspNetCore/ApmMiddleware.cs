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
    public class ApmMiddleware : IDisposable
    {
        private IPayloadSender payloadSender;
        private readonly RequestDelegate next;

        public ApmMiddleware(RequestDelegate next, IPayloadSender payloadSender = null)
        {
            if(payloadSender == null)
            {
                this.payloadSender = new PayloadSender(new Config()); //TODO: Config should be passed from outside
            }
            else
            {
                this.payloadSender = payloadSender;
            }
            this.next = next;
        }

        public void Dispose()
        {
            if (payloadSender is IDisposable dispPayloadSender)
            {
                dispPayloadSender.Dispose();
                dispPayloadSender = null;
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
                                Socket = new Socket{ Encrypted = context.Request.IsHttps, Remote_address = context.Connection?.RemoteIpAddress?.ToString()},
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

            TransactionContainer.Transactions.Value = transactions;

            await next(context);

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

            payload.Transactions = TransactionContainer.Transactions.Value;
            payloadSender.QueuePayload(payload);
        }
    }
}
