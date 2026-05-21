// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;

namespace Elastic.Apm.Report
{
	public interface IPayloadSender
	{
		void QueueError(IError error);

		void QueueMetrics(IMetricSet metrics);

		void QueueSpan(ISpan span);

		void QueueTransaction(ITransaction transaction);
	}

	public interface IPayloadSenderWithFilters
	{
		bool AddFilter(Func<ITransaction, ITransaction> transactionFilter);
		bool AddFilter(Func<ISpan, ISpan> spanFilter);
		bool AddFilter(Func<IError, IError> errorFilter);
	}

	/// <summary>
	/// Implemented by payload senders that support explicit flushing.
	/// Internal only — not part of the public IPayloadSender contract because
	/// netstandard2.0 and .NET Framework targets cannot use default interface members.
	/// </summary>
	internal interface IFlushablePayloadSender
	{
		/// <summary>
		/// Waits until the sender is idle — the event queue is empty and any in-progress HTTP
		/// send has completed — then returns.
		/// </summary>
		/// <remarks>
		/// Completion indicates the send attempt finished; it does not guarantee the server
		/// accepted the data. For short-lived processes, "idle" is equivalent to "all recorded
		/// events transmitted" because no new events arrive after the call.
		/// </remarks>
		Task FlushAsync(CancellationToken cancellationToken = default);
	}
}
