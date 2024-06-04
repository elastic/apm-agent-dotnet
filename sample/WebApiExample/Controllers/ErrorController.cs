using Microsoft.AspNetCore.Mvc;

namespace WebApiExample.Controllers;

[ApiController]
[Route("[controller]")]
public class ErrorController : ControllerBase
{
	private readonly ILogger<ErrorController> _logger;

	public ErrorController(ILogger<ErrorController> logger) => _logger = logger;

	[HttpGet(Name = "GetError")]
	public IEnumerable<WeatherForecast> Get() =>
		throw new Exception("This exception triggers a 500");
}
