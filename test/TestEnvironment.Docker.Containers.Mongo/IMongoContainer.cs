namespace TestEnvironment.Docker.Containers.Mongo
{
    public interface IMongoContainer
    {
        string GetConnectionString();
    }
}
