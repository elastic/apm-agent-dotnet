namespace Elastic.Apm.Api
{
	public struct ApiConstants
	{
		public const string TypeRequest = "request";

		public const string ActionExec = "exec";
		public const string ActionQuery = "query";

		public const string SubtypeHttp = "http";
		public const string SubtypeMssql = "mssql";
		public const string SubtypeSqLite = "sqlite";

		public const string TypeDb = "db";
		public const string TypeExternal = "external";
	}
}
