// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	[RoutePrefix(Path)]
	public class WebApiController : ApiController
	{
		public const string Path = "api/WebApi";

		public WebApiResponse Get() =>
			new WebApiResponse { Content = "This is an example response from a web api controller" };

		[Route]
		[HttpPost]
		public async Task<IHttpActionResult> Post()
		{
			var content = await Request.Content.ReadAsMultipartAsync(); //exception gets thrown here
			return Ok(content.ToString());
		}
	}

	public class WebApiResponse
	{
		public string Content { get; set; }
	}
}
