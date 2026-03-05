using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace SparkService.Services
{
    public class CompatibilityScoresService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<CompatibilityScores> _compatibilityScoresCollection;

        public CompatibilityScoresService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
               happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>(
                happySugarDaddyDatabaseSettings.Value.UsersCollectionName);

            _compatibilityScoresCollection = mongoDatabase.GetCollection<CompatibilityScores>(
                happySugarDaddyDatabaseSettings.Value.CompatibilityScoresCollectionName);
        }


        public async Task CreateAsync(CompatibilityScores userRole) =>
         await _compatibilityScoresCollection.InsertOneAsync(userRole);

        public async Task<CompatibilityScores?> GetAsync(string id) =>
            await _compatibilityScoresCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task UpdateAsync(string Id, CompatibilityScores updatedProfile) =>
         await _compatibilityScoresCollection.ReplaceOneAsync(x => x.Id == Id, updatedProfile);

        public async Task<CompatibilityScores?> GetAsync(string userid, string otherUserid) =>
           await _compatibilityScoresCollection.Find(x => x.user_id == userid && x.other_user_id == otherUserid).FirstOrDefaultAsync();


        public List<CompatibilityScores> GetMemberCompatibilityWithOthers(string userId, int page, int pageSize)
        {
            return _compatibilityScoresCollection.AsQueryable().Where(x => x.user_id == userId)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize).OrderByDescending(x => x.score).ToList();
        }

        public List<CompatibilityScores> GetMembersFromSpecificCompatibilityOnwards(string userId, int score, int page, int pageSize)
        {
            List<CompatibilityScores> compatibilityScores = new List<CompatibilityScores>();

            compatibilityScores = _compatibilityScoresCollection.AsQueryable().Where(x => x.user_id == userId && x.score <= score)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize).OrderByDescending(x => x.score).ToList();

            return compatibilityScores;
        }

        public IQueryable<UserViewModel> ApplyCompatibilityScoreSortingV1(string userId, IQueryable<UserViewModel> membersQueryable)
        {
            return (from member in membersQueryable
                    join compatibility in _compatibilityScoresCollection.AsQueryable()
                    on member.id equals compatibility.other_user_id into groupedMembers
                    from Comptabilities in groupedMembers.Where(x=>x.user_id == userId).DefaultIfEmpty()                   
                    select new
                    {
                        member = member,
                        comptability = Comptabilities

                    }).OrderByDescending(x => x.comptability.score).
            Select(x => x.member).AsQueryable();
        }

    }
}
