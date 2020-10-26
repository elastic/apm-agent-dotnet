// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal interface ICurrentExecutionSegmentsContainer
	{
		/// <summary>
		/// Gets or sets the current span
		/// </summary>
		Span CurrentSpan { get; set; }

		/// <summary>
		/// Gets or sets the current transaction
		/// </summary>
		Transaction CurrentTransaction { get; set; }
	}
}
