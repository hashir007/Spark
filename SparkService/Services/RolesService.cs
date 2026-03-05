using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class RolesService
    {
        private readonly IMongoCollection<Roles> _rolesCollection;

        public RolesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _rolesCollection = mongoDatabase.GetCollection<Roles>(
                happySugarDaddyDatabaseSettings.Value.RolesCollectionName);
        }

        public async Task CreateAsync(Roles role) =>
            await _rolesCollection.InsertOneAsync(role);

        public async Task<List<Roles>> GetAsync() =>
            await _rolesCollection.Find(_ => true).ToListAsync();

        public async Task<Roles> GetByNameAsync(string name) =>
           await _rolesCollection.Find(x => x.name == name).FirstOrDefaultAsync();

        public async Task<Roles> GetAsync(string id) =>
           await _rolesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
}
