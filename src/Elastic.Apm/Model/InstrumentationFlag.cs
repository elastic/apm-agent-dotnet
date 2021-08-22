// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Represents the presence of a given instrumentation module. Each module can set its flag on a given span in
	/// <see cref="Span.InstrumentationFlag" />.
	/// With that every instrumentation module can know which other modules created the given span.
	/// In case of "competing modules" <see cref="Span.InstrumentationFlag" /> can be used to detect if a
	/// competing instrumentation module created the given span.
	/// </summary>
	[Flags]
	internal enum InstrumentationFlag : short
	{
		None = 0,
		HttpClient = 1 << 0,
		AspNetCore = 1 << 1,
		EfCore = 1 << 2,
		EfClassic = 1 << 3,
		SqlClient = 1 << 4,
		AspNetClassic = 1 << 5,
		Azure = 1 << 6,
		Elasticsearch = 1 << 7,
		Postgres = 1 << 8,
		Oracle = 1 << 9,
		MySql = 1 << 10,
		Sqlite = 1 << 11,
	}
}
