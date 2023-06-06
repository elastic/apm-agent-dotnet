// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web.Http;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	[RoutePrefix(RoutePrefix)]
	public class AttributeRoutingWebApiController : ApiController
	{
		public const string RoutePrefix = "api/AttributeRoutingWebApi";
		public const string Route = "{id}";
		public const string RouteAmbiguous = "ambiguous";

		[HttpGet]
		[Route(Route)]
		public IHttpActionResult Get(string id) =>
			Ok($"attributed routed web api controller {id}");

		[HttpGet]
		[Route(RouteAmbiguous)]
		public IHttpActionResult Get2() =>
			Ok($"attributed routed web api controller with ambiguous route");
	}
}
