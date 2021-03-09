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
