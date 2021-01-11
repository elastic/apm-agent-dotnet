// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	[RoutePrefix(Path)]
	public class WebApiController : ApiController
	{
		public const string Path = "api/WebApi";

		[Route]
		[HttpGet]
		public WebApiResponse Get() =>
			new WebApiResponse { Content = "This is an example response from a web api controller" };

		[Route]
		[HttpPost]
		public async Task<IHttpActionResult> Post()
		{
			var multipart = await Request.Content.ReadAsMultipartAsync();
			var result = new StringBuilder();
			foreach(var content in multipart.Contents)
				result.Append(await content.ReadAsStringAsync());

			return Ok(result.ToString());
		}
	}

	public class WebApiResponse
	{
		public string Content { get; set; }
	}
}
