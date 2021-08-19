using System;

namespace Elastic.Apm.Profiler.Managed.Core
{
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
