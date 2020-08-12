// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// An attribute to mark fields for sanitization. This attribute is known to <see cref="ElasticApmContractResolver"/> and it applies a Converter
	/// to sanitize field(s) accordingly.
	/// </summary>
	internal class SanitizationAttribute : Attribute { }
}
