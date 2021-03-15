// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.MongoDb
{
	internal static class Constants
	{
		internal const string MongoDiagnosticName = "MongoDB.Driver";

		public static class Events
		{
			internal const string CommandStart = "CommandStart";
			internal const string CommandEnd = "CommandEnd";
			internal const string CommandFail = "CommandFail";
		}
	}
}
