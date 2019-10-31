using System;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// An attribute to mark fields for sanitization. This attribute is known to <see cref="ElasticApmContractResolver" /> and
	/// it applies a Converter
	/// to sanitize field(s) accordingly.
	/// </summary>
	internal class SanitizationAttribute : Attribute { }
}
