// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Defines the apm server specification that the type adheres to
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
	internal class SpecificationAttribute : Attribute
	{
		public SpecificationAttribute(string path) => Path = path;

		/// <summary>
		/// Path of the specification, relative to the apm-server directory
		/// </summary>
		/// <remarks>
		/// The path also aligns with the APM server specification $id
		/// </remarks>
		public string Path { get; }
	}
}
