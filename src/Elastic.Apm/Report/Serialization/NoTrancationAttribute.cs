using System;

namespace Elastic.Apm.Report.Serialization
{
	[AttributeUsage(AttributeTargets.Property)]
	public class NoTruncationAttribute : Attribute { }
}
