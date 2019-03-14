using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Microsoft.AspNetCore.Http;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.AspNetCore
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class ApmMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly Tracer _tracer;
		private readonly IConfigurationReader _configurationReader;

		public ApmMiddleware(RequestDelegate next, Tracer tracer, IConfigurationReader configurationReader)
		{
			_next = next;
			_tracer = tracer;
			_configurationReader = configurationReader;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var transaction = _tracer.StartTransactionInternal($"{context.Request.Method} {context.Request.Path}",
				ApiConstants.TypeRequest);

			//Get traceparent and parse

			var url = new Url
			{
				Full = context.Request?.Path.Value,
				HostName = context.Request.Host.Host,
				Protocol = GetProtocolName(context.Request.Protocol),
				Raw = context.Request?.Path.Value //TODO
			};

			Dictionary<string, string> requestHeaders = null;
			if (_configurationReader.CaptureHeaders)
			{
				requestHeaders = new Dictionary<string, string>();

				foreach (var header in context.Request.Headers)
					requestHeaders.Add(header.Key, header.Value.ToString());
			}

			transaction.Context.Request = new Request(context.Request.Method, url)
			{
				Socket = new Socket
				{
					Encrypted = context.Request.IsHttps,
					RemoteAddress = context.Connection?.RemoteIpAddress?.ToString()
				},
				HttpVersion = GetHttpVersion(context.Request.Protocol),
				Headers = requestHeaders
			};

			try
			{
				await _next(context);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				Dictionary<string, string> responseHeaders = null;

				if (_configurationReader.CaptureHeaders)
				{
					responseHeaders = new Dictionary<string, string>();

					foreach (var header in context.Response.Headers)
						responseHeaders.Add(header.Key, header.Value.ToString());
				}

				transaction.Result = $"{GetProtocolName(context.Request.Protocol)} {context.Response.StatusCode.ToString()[0]}xx";
				transaction.Context.Response = new Response
				{
					Finished = context.Response.HasStarted, //TODO ?
					StatusCode = context.Response.StatusCode,
					Headers = responseHeaders
				};

				transaction.End();
			}
		}

		private string GetProtocolName(string protocol)
		{
			switch (protocol)
			{
				case string s when string.IsNullOrEmpty(s):
					return string.Empty;
				case string s when s.StartsWith("HTTP", StringComparison.InvariantCulture): //in case of HTTP/2.x we only need HTTP
					return "HTTP";
				default:
					return protocol;
			}
		}

		private string GetHttpVersion(string protocolString)
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
					return protocolString.Replace("HTTP/", string.Empty);
			}
		}
	}
}
