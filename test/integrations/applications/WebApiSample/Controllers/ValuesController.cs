// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ValuesController : ControllerBase
	{
		// GET api/values
		[HttpGet]
		public async Task<ActionResult<IEnumerable<string>>> Get()
		{
			var lastValue = "call to elastic.co: ";
			try
			{
				var httpClient = new HttpClient();
				var elasticRes = await httpClient.GetAsync("https://elastic.co");
				lastValue += elasticRes.StatusCode;
			}
			catch
			{
				lastValue += "failed";
			}

			return new[] { "value1", "value2", lastValue };
		}

		// GET api/values/5
		[HttpGet("{id}")]
		public ActionResult<string> Get(int id) => "value" + id;
	}
}
