using System.Threading.Tasks;
using MongoDB.Driver;

namespace Elastic.Apm.Mongo.IntegrationTests.Fixture
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
