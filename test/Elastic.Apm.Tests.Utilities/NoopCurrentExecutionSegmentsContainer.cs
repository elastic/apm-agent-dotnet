// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;

namespace Elastic.Apm.Tests.Utilities
{
	internal class NoopCurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		public ISpan CurrentSpan { get; set; }
		public ITransaction CurrentTransaction { get; set; }
	}
}
