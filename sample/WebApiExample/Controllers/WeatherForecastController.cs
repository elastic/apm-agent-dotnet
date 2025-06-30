using System.Diagnostics;
using Elastic.Apm;
using Microsoft.AspNetCore.Mvc;

namespace WebApiExample.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
	private static readonly string[] Summaries =
	[
		"Freezing",
		"Bracing",
		"Chilly",
		"Cool",
		"Mild",
		"Warm",
		"Balmy",
		"Hot",
		"Sweltering",
		"Scorching"
	];

	[HttpGet(Name = "GetWeatherForecast")]
	public  async Task<IEnumerable<WeatherForecast>> Get()
	{
		var transaction = Agent.Tracer.StartTransaction("ExampleTransaction", "CustomType");
		try
		{
			// Your application logic here
			transaction.CaptureSpan("ExampleSpan", "CustomSpanType", () =>
			{
				// Span logic here
			});
		}
		catch (Exception ex)
		{
			transaction.CaptureException(ex);
		}
		finally
		{
			transaction.End();
		}

		var dd = Random.Shared.Next(100);
		if (dd > 10)
		{
			// Create an HttpClient to make an outgoing HTTP request
			var httpClient = new HttpClient();

			// Make an HTTP GET request
			Console.WriteLine("Making an HTTP GET request...");
			var response = await httpClient.GetAsync("https://localhost:55180/WeatherForecast");

			// Read and display the response
			var content = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"Response: {content}");
		}

		return
			Enumerable.Range(1, 5)
				.Select(index => new WeatherForecast
				{
					Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
					TemperatureC = Random.Shared.Next(-20, 55),
					Summary = Summaries[Random.Shared.Next(Summaries.Length)]
				})
				.ToArray();
	}

}
