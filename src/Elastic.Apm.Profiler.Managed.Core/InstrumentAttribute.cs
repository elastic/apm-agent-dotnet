// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Elastic.Apm.Profiler.Managed.Core
{
	/// <summary>
	/// Decorated class instruments a method
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class InstrumentAttribute : Attribute
	{
		/// <summary>
		/// The name of the group to which this instrumentation belongs
		/// </summary>
		public string Group { get; set; }

		/// <summary>
		/// The name of the assembly containing the target method to instrument
		/// </summary>
		public string Assembly { get; set; }

		/// <summary>
		/// The name of the nuget package containing the assembly to instrument.
		/// Used for documentation. If unspecified, will use <see cref="Assembly"/> value.
		/// Values starting with "part of" are specially treated.
		/// </summary>
		public string Nuget { get; set; }

		/// <summary>
		/// The fully qualified name of the type containing the target method to instrument
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// The name of the method to instrument
		/// </summary>
		public string Method { get; set; }

		/// <summary>
		/// The fully qualified name of the return type of the method to instrument
		/// </summary>
		public string ReturnType { get; set; }

		/// <summary>
		/// The fully qualified names of the parameter types of the method to instrument
		/// </summary>
		public string[] ParameterTypes { get; set; }

		/// <summary>
		/// The minimum assembly version that can be instrumented
		/// </summary>
		public string MinimumVersion { get; set; }

		/// <summary>
		/// The maximum assembly version that can be instrumented
		/// </summary>
		public string MaximumVersion { get; set; }

		/// <summary>
		/// The type to which this instrumentation applies. If null, the type will
		/// be determined from the type to which the attribute is applied.
		/// </summary>
		public Type CallTargetType { get; set; }
	}
}
