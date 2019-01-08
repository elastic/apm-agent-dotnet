using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model.Payload;
using Microsoft.AspNetCore.Http;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

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
				Agent = new Service.AgentC
				{
					Name = Consts.AgentName,
					Version = Consts.AgentVersion
				},
				Name = Assembly.GetEntryAssembly()?.GetName()?.Name,
				Framework = new Framework { Name = "ASP.NET Core", Version = "2.1" }, //TODO: Get version
				Language = new Language { Name = "C#" } //TODO
			};

			Agent.Tracer.Service = service;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var transaction = Agent.Tracer.StartTransaction($"{context.Request.Method} {context.Request.Path}",
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

			try
			{
				await next(context);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.Result =
					$"{GetProtocolName(context.Request.Protocol)} {context.Response.StatusCode.ToString()[0]}xx";
				transaction.Context.Response = new Response
				{
					Finished = context.Response.HasStarted, //TODO ?
					Status_code = context.Response.StatusCode
				};

				transaction.End();
			}
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
