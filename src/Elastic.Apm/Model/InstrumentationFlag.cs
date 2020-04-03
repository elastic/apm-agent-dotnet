using System;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Represents a given instrumentation module. Each module can set its flag on a given transaction in
	/// <see cref="Transaction.ActiveInstrumentationFlags" />.
	/// With that every instrumentation module can know which other modules created spans on the given transaction.
	/// In case of "competing modules" <see cref="Transaction.ActiveInstrumentationFlags" /> can be used to detect if a
	/// competing instrumentation module already created spans on the given transaction.
	/// </summary>
	[Flags]
	internal enum InstrumentationFlag : short
	{
		None = 0,
		HttpClient = 1,
		AspNetCore = 2,
		EfCore = 4,
		EfClassic = 8,
		SqlClient = 16,
		AspNetClassic = 32
	}
}
