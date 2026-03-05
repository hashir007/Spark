using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class KissesService
    {
        private readonly IMongoCollection<Kisses> _kissesCollection;
        private readonly UsersService _usersService;

        public KissesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService)
        {

            var mongoClient = new MongoClient(
                   happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _kissesCollection = mongoDatabase.GetCollection<Kisses>(
                happySugarDaddyDatabaseSettings.Value.KissesCollectionName);

            _usersService = usersService;
        }

        public async Task AddKisses(Kisses model)
        {
            await _kissesCollection.InsertOneAsync(model);
        }

        public List<KissesViewModel> GetKisses(string userid)
        {
            List<KissesViewModel> favorites = new List<KissesViewModel>();

            favorites = _kissesCollection.AsQueryable().Where(x => x.user_id == userid).Select(x => new KissesViewModel
            {
                Id = x.Id,
                kissed_id = x.kissed_id,
                kissed_count = x.kissed_count,
                user_id = x.user_id,
                created_at = x.created_at,
                updated_at = x.updated_at,
            }).ToList();

            favorites.ForEach(x => { x.kissed = _usersService.GetDetailedV2(x.kissed_id!)!; x.user = _usersService.GetDetailedV2(x.user_id!)!; });

            return favorites;
        }

        public async Task<Kisses> GetAsync(string userid, string favorite_id)
        {
            return await _kissesCollection.Find(x => x.user_id == userid && x.kissed_id == favorite_id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(string id, Kisses updatedBook) =>
          await _kissesCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public List<KissesViewModel> GetKissesReceived(string userid)
        {
            List<KissesViewModel> kisses = new List<KissesViewModel>();

            kisses = _kissesCollection.AsQueryable().Where(x => x.kissed_id == userid).Select(x => new KissesViewModel
            {
                Id = x.Id,
                kissed_id = x.kissed_id,
                kissed_count = x.kissed_count,
                user_id = x.user_id,
                created_at = x.created_at,
                updated_at = x.updated_at,
            }).ToList();

            kisses.ForEach(x => { x.kissed = _usersService.GetDetailedV2(x.kissed_id!)!;
                x.user = _usersService.GetDetailedV2(x.user_id!)!; });

            return kisses;
        }

        public long GetKissesCount(string userId) => _kissesCollection.Find(x => x.user_id == userId).CountDocuments(); 
    }
}
