// <copyright file="IConsumeResult.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
	/// <summary>
	/// ConsumeResult for duck-typing
	/// </summary>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IConsumeResult
	{
		/// <summary>
		/// Gets the topic
		/// </summary>
		string Topic { get; }

		/// <summary>
		/// Gets the partition
		/// </summary>
		Partition Partition { get; }

		/// <summary>
		/// Gets the offset
		/// </summary>
		Offset Offset { get; }

		/// <summary>
		/// Gets the Kafka record
		/// </summary>
		IMessage Message { get; }

		/// <summary>
		/// Gets a value indicating whether gets whether the message is a partition EOF
		/// </summary>
		// ReSharper disable once InconsistentNaming
		bool IsPartitionEOF { get; }
	}
}
