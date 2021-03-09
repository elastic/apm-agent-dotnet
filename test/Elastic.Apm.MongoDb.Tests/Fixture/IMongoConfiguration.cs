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
