using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Elastic.Apm.Tests.MockApmServer
{
	[Route("intake/v2/events")]
	[ApiController]
	public class IntakeV2EventsController : ControllerBase
	{
		private readonly MockApmServer _mockApmServer;

		IntakeV2EventsController(MockApmServer mockApmServer)
		{
			_mockApmServer = mockApmServer;
		}

		[HttpPost]
		public ActionResult<string> Post([FromBody] string bodyContent)
		{
			return $"Received bodyContent.Length: {bodyContent.Length}";
		}
	}
}
