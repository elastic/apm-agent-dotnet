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

		public async Task<IEnumerable<string>> GetValue(int id)
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

			return new[] { $"value{id}", lastValue };
		}

		public Model CreateValue(Model model) => model;
	}

	public class Model
	{
		public int Id { get; set; }

		public string Name { get; set; }
	}
}
