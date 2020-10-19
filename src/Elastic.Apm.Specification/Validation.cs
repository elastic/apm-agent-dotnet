// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Specification
{
	/// <summary>
	/// Determines how validation is performed
	/// </summary>
	public enum Validation
	{
		/// <summary>
		/// Validates the type against the specification. A type must be a valid implementation of the specification, but
		/// it may be only a subset of the properties i.e. required properties only.
		/// </summary>
		TypeToSpec,
		/// <summary>
		/// Validates the specification against the type. A type must match the specification exactly in order to be valid
		/// </summary>
		/// <remarks>
		/// It's expected that the type is the implementation of the entire specification and not a subset
		/// of certain optional properties.
		/// </remarks>
		SpecToType
	}
}
