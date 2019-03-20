using System;
using System.Collections.Generic;
using System.Linq;
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
			var httpClient = new HttpClient();
			var elasticRes = await httpClient.GetAsync("https://elastic.co");
			return new string[] { "value1", "value2", elasticRes.IsSuccessStatusCode.ToString() };
		}

		// GET api/values/5
		[HttpGet("{id}")]
		public ActionResult<string> Get(int id)
		{
			return "value";
		}

		// POST api/values
		[HttpPost]
		public void Post([FromBody] string value) { }

		// PUT api/values/5
		[HttpPut("{id}")]
		public void Put(int id, [FromBody] string value) { }

		// DELETE api/values/5
		[HttpDelete("{id}")]
		public void Delete(int id) { }
	}
}
