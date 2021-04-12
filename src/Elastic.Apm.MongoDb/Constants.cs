// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Elastic.Apm.MongoDb
{
	internal static class Constants
	{
		internal const string MongoDiagnosticName = "MongoDB.Driver";

		public static class Events
		{
			internal const string CommandStart = nameof(CommandStart);
			internal const string CommandEnd = nameof(CommandEnd);
			internal const string CommandFail = nameof(CommandFail);
		}
	}
}
