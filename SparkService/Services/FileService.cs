using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class FileService
    {
        private readonly IMongoCollection<Models.File> _filesCollection;

        public FileService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _filesCollection = mongoDatabase.GetCollection<Models.File>(
                happySugarDaddyDatabaseSettings.Value.FileCollectionName);
        }

        public async Task CreateAsync(Models.File newBook) =>
          await _filesCollection.InsertOneAsync(newBook);


    }
}
