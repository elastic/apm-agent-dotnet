using System;
using System.Text;
using System.Web;
using Elastic.Apm.Logging;


namespace Elastic.Apm.AspNetFullFramework.Extensions
{
	internal static class HttpRequestExtensions
	{
		private const string SoapActionHeaderName = "SOAPAction";

		/// <summary>
		/// Extracts the soap action from the header if exists only with Soap 1.1
		/// </summary>
		/// <param name="request">The request.</param>
		/// <param name="logger">The logger.</param>
		public static string ExtractSoapAction(this HttpRequest request, IApmLogger logger)
		{
			try
			{
				var soapActionWithNamespace = request.Headers.Get(SoapActionHeaderName);

				if (string.IsNullOrWhiteSpace(soapActionWithNamespace)) return null;

				var indexPosition = soapActionWithNamespace.LastIndexOf(@"/");
				if (indexPosition != -1)
				{
					return soapActionWithNamespace.Substring(indexPosition + 1).TrimEnd('\"');
				}
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error reading soap action header");
			}

			return null;
		}
	}
}
