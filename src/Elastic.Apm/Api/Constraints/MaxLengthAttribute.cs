// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Api.Constraints
{
	/// <summary>
	/// Specifies the maximum length of string data allowed in a property, based on the APM server specification.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class MaxLengthAttribute : Attribute
	{
		/// <summary>
		/// The maximum length.
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Instantiates a new instance of <see cref="MaxLengthAttribute"/>
		/// with a maximum length of <see cref="Consts.PropertyMaxLength"/>
		/// </summary>
		public MaxLengthAttribute() : this(Consts.PropertyMaxLength)
		{
		}

		/// <summary>
		/// Instantiates a new instance of <see cref="MaxLengthAttribute"/> with a given maximum length
		/// </summary>
		public MaxLengthAttribute(int maxLength) => Length = maxLength;
	}
}
