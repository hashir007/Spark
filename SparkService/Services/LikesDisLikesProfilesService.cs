using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class LikesDisLikesProfilesService
    {
        private readonly IMongoCollection<LikesDisLikesProfiles> _likesDisLikesProfilesCollection;
        private readonly UsersService _usersService;

        public LikesDisLikesProfilesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService)
        {
            var mongoClient = new MongoClient(
                happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _likesDisLikesProfilesCollection = mongoDatabase.GetCollection<LikesDisLikesProfiles>(
                happySugarDaddyDatabaseSettings.Value.LikesDisLikesProfilesCollectionName);

            _usersService = usersService;
        }

        public async Task AddLikesDisLikes(LikesDisLikesProfiles likesDisLikesProfiles)
        {
            await _likesDisLikesProfilesCollection.InsertOneAsync(likesDisLikesProfiles);
        }

        public List<LikesDisLikesProfilesViewModel> GetLikesForUser(string userid)
        {
            List<LikesDisLikesProfilesViewModel> likesDisLikesProfiles = new List<LikesDisLikesProfilesViewModel>();

            likesDisLikesProfiles = _likesDisLikesProfilesCollection.AsQueryable().Where(x => x.user_id == userid && x.isLikes == true).Select(x => new LikesDisLikesProfilesViewModel
            {
                Id = x.Id,
                isLikes = x.isLikes,
                profile_id = x.profile_id,
                user_id = x.user_id,
                updated_at = x.updated_at,
                created_at = x.created_at,
            }).ToList();

            likesDisLikesProfiles.ForEach(x => { x.profile = _usersService.GetDetailedV2(x.profile_id!)!; x.user = _usersService.GetDetailedV2(x.user_id!)!; });

            return likesDisLikesProfiles;
        }

        public List<LikesDisLikesProfilesViewModel> GetDisLikesForUser(string userid)
        {
            List<LikesDisLikesProfilesViewModel> likesDisLikesProfiles = new List<LikesDisLikesProfilesViewModel>();

            likesDisLikesProfiles = _likesDisLikesProfilesCollection.AsQueryable().Where(x => x.user_id == userid && x.isLikes == false).Select(x => new LikesDisLikesProfilesViewModel
            {
                Id = x.Id,
                isLikes = x.isLikes,
                profile_id = x.profile_id,
                user_id = x.user_id,
                created_at = x.created_at,
                updated_at = x.updated_at
            }).ToList();

            likesDisLikesProfiles.ForEach(x => { x.profile = _usersService.GetDetailedV2(x.profile_id!)!; x.user = _usersService.GetDetailedV2(x.user_id!)!; });

            return likesDisLikesProfiles;
        }

        public async Task<LikesDisLikesProfiles> GetAsync(string userid, string profile_id)
        {
            return await _likesDisLikesProfilesCollection.Find(x => x.user_id == userid && x.profile_id == profile_id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(string id, LikesDisLikesProfiles updatedBook) =>
           await _likesDisLikesProfilesCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public long GetLikesCount(string userId) => _likesDisLikesProfilesCollection.Find(x => x.user_id == userId && x.isLikes).CountDocuments();

        public long GetDislikesCount(string userId) => _likesDisLikesProfilesCollection.Find(x => x.user_id == userId && !x.isLikes).CountDocuments();
    }
}
