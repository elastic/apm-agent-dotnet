// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MongoDB.Driver;

namespace Elastic.Apm.MongoDb.Tests.Fixture
{
	public interface IMongoConfiguration<TDocument>
	{
		string DatabaseName { get; }

		string CollectionName { get; }
		MongoClient GetMongoClient(string connectionString);

		Task InitializeAsync(IMongoCollection<TDocument> collection);

		Task DisposeAsync(IMongoCollection<TDocument> collection);
	}
}
