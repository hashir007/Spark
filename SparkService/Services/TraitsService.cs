using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class TraitsService
    {
        private readonly IMongoCollection<Traits> _traitsCollection;
        private readonly IMongoCollection<UserTraits> _userTraitsCollection;
        private readonly UsersService _usersService;

        public TraitsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService)
        {
            var mongoClient = new MongoClient(
                 happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _traitsCollection = mongoDatabase.GetCollection<Traits>(
                happySugarDaddyDatabaseSettings.Value.TraitsCollectionName);

            _userTraitsCollection = mongoDatabase.GetCollection<UserTraits>(
             happySugarDaddyDatabaseSettings.Value.UserTraitsCollectionName);
            _usersService = usersService;
        }

        public async Task AddTraits(Traits model)
        {
            await _traitsCollection.InsertOneAsync(model);
        }

        public async Task AddUserTraits(UserTraits model)
        {
            await _userTraitsCollection.InsertOneAsync(model);
        }

        public async Task UpdateUserTraitsAsync(string id, UserTraits updateConversation) =>
          await _userTraitsCollection.ReplaceOneAsync(x => x.Id == id, updateConversation);

        public List<UserTraitsViewModel> GetUserTraits(string userId)
        {
            var traits = (from ut in _userTraitsCollection.AsQueryable()
                          where ut.user_id == userId
                          select new UserTraitsViewModel
                          {
                              trait_id = ut.trait_id,
                              user_id = ut.user_id,
                              trait_value = ut.trait_value
                          }).ToList();

            traits.ForEach(x => { x.trait = GetTrait(x.trait_id); x.user = _usersService.GetDetailedV2(x.user_id!)!; });

            return traits;
        }

        public Traits GetTrait(string id)
        {
            return _traitsCollection.Find(x => x.Id == id).FirstOrDefault();
        }

        public List<Traits> GetAllTraits()
        {
            return _traitsCollection.Find(_ => true).ToList();
        }

        public UserTraits GetUserTraitsById(string userId,string traitId)
        {
            return _userTraitsCollection.Find(x => x.user_id == userId && x.trait_id == traitId).FirstOrDefault();
        }

       
    }
}
