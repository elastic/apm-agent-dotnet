// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Api
{
	public interface IError
	{
		string Culprit { get; set; }

		CapturedException Exception { get; }
		string Id { get; }

		string ParentId { get; }

		string TraceId { get; }

		string TransactionId { get; }
	}
}
