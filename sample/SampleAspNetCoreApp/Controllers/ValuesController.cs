using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SampleAspNetCoreApp.Controllers
{
	/**
	 * The values controller contains a post method for testing the 'CaptureBody' config option of the dotnet apm module
	 */
	[Route("api/Values")]
	public class ValuesController : Controller
	{
		// GET api/values
		//[HttpGet]
		//public async Task<ActionResult<IEnumerable<string>>> Get()
		//{
		//	var lastValue = "call to elastic.co: ";
		//	try
		//	{
		//		var httpClient = new HttpClient();
		//		var elasticRes = await httpClient.GetAsync("https://elastic.co");
		//		lastValue += elasticRes.StatusCode;
		//	}
		//	catch
		//	{
		//		lastValue += "failed";
		//	}

		//	return new[] { "value1", "value2", lastValue };
		//}

		//// GET api/values/5
		//[HttpGet("{id}")]
		//public ActionResult<string> Get(int id) => "value" + id;

		[HttpPost("{id}")]
		public ActionResult<string> Post([FromForm]string id) => "value=" + id;
	}
}
