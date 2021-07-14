// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Elastic.Apm.Profiler.Managed.Reflection;

namespace Elastic.Apm.Profiler.Managed.Integrations
{
	public static class HttpMessageHandlerIntegration
	{
		private const string SystemNetHttp = "System.Net.Http";
		private const string Major4 = "4";
		private const string Major5 = "5";

		private const string HttpMessageHandlerTypeName = "HttpMessageHandler";
		private const string HttpClientHandlerTypeName = "HttpClientHandler";

		private const string HttpMessageHandler = SystemNetHttp + "." + HttpMessageHandlerTypeName;
		private const string HttpClientHandler = SystemNetHttp + "." + HttpClientHandlerTypeName;
		private const string SendAsync = "SendAsync";
		private const string Send = "Send";

		private static readonly string[] NamespaceAndNameFilters = { ClrNames.HttpResponseMessageTask, ClrNames.HttpRequestMessage, ClrNames.CancellationToken };

		public static object HttpMessageHandler_SendAsync(
		    object handler,
			object request,
			object boxedCancellationToken,
			int opCode,
			int mdToken,
			long moduleVersionPtr
		)
		{
			if (handler == null) throw new ArgumentNullException(nameof(handler));

			var cancellationToken = (CancellationToken)boxedCancellationToken;
			var httpMessageHandler = handler.GetInstrumentedType(SystemNetHttp, HttpMessageHandlerTypeName);

			Func<object, object, CancellationToken, object> instrumentedMethod = null;

			try
			{
				instrumentedMethod =
					MethodBuilder<Func<object, object, CancellationToken, object>>
						.Start(moduleVersionPtr, mdToken, opCode, SendAsync)
						.WithConcreteType(httpMessageHandler)
						.WithParameters(request, cancellationToken)
						.WithNamespaceAndNameFilters(NamespaceAndNameFilters)
						.Build();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"exception building method: {ex}");
				Console.Out.Flush();
				throw;
			}

			return instrumentedMethod(handler, request, cancellationToken);
		}

		public static object HttpClientHandler_SendAsync(
			object handler,
			object request,
			object boxedCancellationToken,
			int opCode,
			int mdToken,
			long moduleVersionPtr
		)
		{
			if (handler == null) throw new ArgumentNullException(nameof(handler));

			var cancellationToken = (CancellationToken)boxedCancellationToken;
			var callOpCode = (OpCodeValue)opCode;
			var httpMessageHandler = handler.GetInstrumentedType(SystemNetHttp, HttpMessageHandlerTypeName);
			Func<object, object, CancellationToken, object> instrumentedMethod = null;

			try
			{
				instrumentedMethod =
					MethodBuilder<Func<object, object, CancellationToken, object>>
						.Start(moduleVersionPtr, mdToken, opCode, SendAsync)
						.WithConcreteType(httpMessageHandler)
						.WithParameters(request, cancellationToken)
						.WithNamespaceAndNameFilters(NamespaceAndNameFilters)
						.Build();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"exception building method: {ex}");
				Console.Out.Flush();
				throw;
			}

			return instrumentedMethod(handler, request, cancellationToken);
		}
	}
}
