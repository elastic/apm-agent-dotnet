// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web.Http;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	public class WebApiController : ApiController
	{
		public const string Path = "api/WebApi";

		public WebApiResponse Get() =>
			new WebApiResponse { Content = "This is an example response from a web api controller" };
	}

	public class WebApiResponse
	{
		public string Content { get; set; }
	}
}
