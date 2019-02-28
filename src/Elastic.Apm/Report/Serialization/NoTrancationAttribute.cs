using System;

namespace Elastic.Apm.Report.Serialization
{
	/// <inheritdoc />
	/// <summary>
	/// By marking a property with this attribute you can tell <see cref="StringTruncationValueResolver"/> to not
	/// trim the string value, even if it's longer than <see cref="Consts.PropertyMaxLength"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	internal class NoTruncationInJsonNetAttribute : Attribute { }
}
