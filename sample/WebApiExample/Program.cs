
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;




//// Step 2: Initialize Elastic APM
//Agent.Subscribe(new HttpDiagnosticsSubscriber());

//// Step 3: Output a confirmation message
//Console.WriteLine("Elastic APM initialized. Logs should now appear in the console.");

//// Step 4: Simulate some activity (e.g., a transaction)
//var transaction = Agent.Tracer.StartTransaction("TestTransaction", "Custom");
//transaction.End();

//Console.WriteLine("Transaction completed.");

//return;

// Step 1: Explicitly configure Elastic APM to log to the console
//Environment.SetEnvironmentVariable("ELASTIC_APM_LOG_FILE", "-"); // Log to console
//Environment.SetEnvironmentVariable("ELASTIC_APM_LOG_LEVEL", "Debug"); // Set log level to Debug


//Agent.Subscribe(new HttpDiagnosticsSubscriber()); // Subscribes to HttpClient diagnostics

//// Set up an HTTP Client
//var httpClient = new HttpClient();

//// Start a custom transaction
//var transaction = Agent.Tracer.StartTransaction("HttpClientExampleTransaction", ApiConstants.TypeRequest);

//try
//{
//	// Make an HTTP GET request
//	Console.WriteLine("Making an HTTP GET request...");
//	var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts/1");

//	// Read and display the response
//	var content = await response.Content.ReadAsStringAsync();
//	Console.WriteLine($"Response: {content}");

//	// Mark the transaction as successful
//	transaction.Result = "HTTP 2xx";
//}
//catch (Exception ex)
//{
//	// Capture the exception in Elastic APM
//	transaction.CaptureException(ex);
//	transaction.Result = "HTTP 5xx";
//}
//finally
//{
//	// End the transaction
//	transaction.End();
//}
//return;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAllElasticApm();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
