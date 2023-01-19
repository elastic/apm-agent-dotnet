// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Api
{
	public struct ApiConstants
	{
		public const string ActionExec = "exec";
		public const string ActionQuery = "query";

		public const string SubtypeElasticsearch = "elasticsearch";
		public const string SubtypeHttp = "http";
		public const string SubtypeMssql = "mssql";
		public const string SubtypeSqLite = "sqlite";
		public const string SubtypeMySql = "mysql";
		public const string SubtypeOracle = "oracle";
		public const string SubtypePostgreSql = "postgresql";
		public const string SubTypeGrpc = "grpc";
		public const string SubTypeRedis = "redis";
		public const string SubTypeMongoDb = "mongodb";
		public const string SubTypeCosmosDb = "cosmosdb";
		public const string SubTypeInternal = "internal";

		public const string TypeRequest = "request";
		public const string TypeDb = "db";
		public const string TypeExternal = "external";
		public const string TypeMessaging = "messaging";
		public const string TypeStorage = "storage";
		public const string TypeApp = "app";
		public const string TypeUnknown = "unknown";
	}
}
