// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Specification
{
	/// <summary>
	/// A property defined on a type that implements the APM server specification
	/// </summary>
	public class ImplementationProperty
	{
		public ImplementationProperty(string name, Type propertyType, Type declaringType)
		{
			Name = name;
			PropertyType = propertyType;
			DeclaringType = declaringType;
		}

		/// <summary>
		/// The name of the property
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The type of the property
		/// </summary>
		public Type PropertyType { get; }

		/// <summary>
		/// The type that declares the property
		/// </summary>
		public Type DeclaringType { get; }

		/// <summary>
		/// The max length that the property value can have
		/// </summary>
		public int? MaxLength { get; set;  }
	}
}
