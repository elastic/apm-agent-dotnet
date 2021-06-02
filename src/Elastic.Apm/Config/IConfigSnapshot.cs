// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Config
{
	/// <summary>
	/// This represents the snapshot of the merged local and central configuration values.
	/// An instance of this is attached to each <see cref="Api.ITransaction" /> which holds a snapshot of the
	/// merged config values from the point in time when the transaction started.
	/// In case central config changes in the middle of a transaction, this snapshot won't chance. Instead changes will be
	/// applied when the next transaction is created with its new snapshot.
	/// </summary>
	public interface IConfigSnapshot : IConfigurationReader
	{
		string DbgDescription { get; }
	}
}
