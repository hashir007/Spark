using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class UserRolesService
    {
        private readonly IMongoCollection<UserRoles> _userRolesCollection;

        public UserRolesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _userRolesCollection = mongoDatabase.GetCollection<UserRoles>(
                happySugarDaddyDatabaseSettings.Value.UserRolesCollectionName);
        }

        public async Task CreateAsync(UserRoles userRole) =>
          await _userRolesCollection.InsertOneAsync(userRole);

        public async Task<UserRoles?> GetAsync(string id) =>
            await _userRolesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<List<UserRoles>> GetByUserIdAsync(string userId) =>
          await _userRolesCollection.Find(x => x.UserId == userId).ToListAsync();


    }
}
