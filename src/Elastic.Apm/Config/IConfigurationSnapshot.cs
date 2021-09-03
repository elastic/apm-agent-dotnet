// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;

namespace Elastic.Apm.Config
{
	/// <summary>
	/// A snapshot of agent configuration containing values
	/// initial configuration combined with dynamic values from central configuration, if enabled.
	/// </summary>
	public interface IConfigurationSnapshot : IConfigurationReader
	{
	}

	/// <summary>
	/// A description for the configuration snapshot
	/// </summary>
	internal interface IConfigurationSnapshotDescription
	{
		public string Description { get; }
	}

	internal static class ConfigurationSnapshotExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Description(this IConfigurationSnapshot snapshot) =>
			snapshot is IConfigurationSnapshotDescription snapshotWithDescription
				? snapshotWithDescription.Description
				: null;
	}
}
