using System.Diagnostics;

namespace WorkerServiceSample
{
	public class Worker : BackgroundService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private static readonly ActivitySource ActivitySource = new("MyActivitySource");

		public Worker(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				using var activity = ActivitySource.StartActivity("UnitOfWork");
				var client = _httpClientFactory.CreateClient();
				await client.GetAsync("https://www.elastic.co", stoppingToken);
				await Task.Delay(5000, stoppingToken);
			}
		}
	}
}
