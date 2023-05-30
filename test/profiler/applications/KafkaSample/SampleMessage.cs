// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on Producer type in https://github.com/DataDog/dd-trace-dotnet/blob/master/tracer/test/test-applications/integrations/Samples.Kafka/Producer.cs#L107-L119
// Licensed under Apache 2.0

namespace KafkaSample
{
	public class SampleMessage
	{
		public string Category { get; }
		public int MessageNumber { get; }
		public bool IsProducedAsync { get; }

		public SampleMessage(string category, int messageNumber, bool isProducedAsync)
		{
			Category = category;
			MessageNumber = messageNumber;
			IsProducedAsync = isProducedAsync;
		}
	}
}
