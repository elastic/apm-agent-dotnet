using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SampleHttpSelfHostApp.Controllers
{
	public class ValuesController : ApiController
	{
		public async Task<IEnumerable<string>> GetValues()
		{
			var lastValue = "call to elastic.co: ";
			try
			{
				using (var httpClient = new HttpClient())
				{
					var elasticRes = await httpClient.GetAsync("https://elastic.co");
					lastValue += elasticRes.StatusCode;
				}
			}
			catch
			{
				lastValue += "failed";
			}

			return new[] { "value1", "value2", lastValue };
		}
	}
}
