## HttpListenerSample ##

The goal of this sample is to show you how you can use the Public Agent API for frameworks that are currently not covered by auto-instrumentation features.

This is a simple application that listens to HTTP requests on http://localhost:8080 and does two things within each request:
- It generates a random number
- It returns the number of stars on the Elastic APM .NET Agent GitHub repository 

The Elastic APM agent traces the incoming requests with the Public Agent API. 

The agent starts a transaction for every incoming request and it creates a span with the Public Agent API each time the GenerateRandomNumber method is executed. The HTTP call in the GetNumberOfStars method is automatically captured by the agent.