// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.WebHost;

namespace AspNetFullFrameworkSampleApp
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration configuration)
		{
			// remove xml support
			var appXmlType = configuration.Formatters.XmlFormatter.SupportedMediaTypes
				.FirstOrDefault(t => t.MediaType == "application/xml");
			configuration.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

			// don't buffer multipart data to web api
			configuration.Services.Replace(typeof(IHostBufferPolicySelector), new NoBufferMultipartPolicySelector());
		}
	}

	public class NoBufferMultipartPolicySelector : WebHostBufferPolicySelector
	{
		public override bool UseBufferedInputStream(object hostContext)
		{
			if (hostContext is HttpContextBase contextBase &&
				contextBase.Request.ContentType != null &&
				contextBase.Request.ContentType.Contains("multipart"))
				return false;

			return base.UseBufferedInputStream(hostContext);
		}
	}
}
