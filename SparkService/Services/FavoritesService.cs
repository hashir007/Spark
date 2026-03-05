using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class FavoritesService
    {
        private readonly IMongoCollection<Favorites> _favoritesCollection;
        private readonly UsersService _usersService;

        public FavoritesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService)
        {

            var mongoClient = new MongoClient(
                   happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _favoritesCollection = mongoDatabase.GetCollection<Favorites>(
                happySugarDaddyDatabaseSettings.Value.FavoritesCollectionName);

            _usersService = usersService;
        }

        public async Task AddFavoritess(Favorites model)
        {
            await _favoritesCollection.InsertOneAsync(model);
        }

        public List<FavoritessViewModel> GetFavoritess(string userid)
        {
            List<FavoritessViewModel> favorites = new List<FavoritessViewModel>();

            favorites = _favoritesCollection.AsQueryable().Where(x => x.user_id == userid).Select(x => new FavoritessViewModel
            {
                Id = x.Id,
                favorite_id = x.favorite_id,
                user_id = x.user_id,
                updated_at = x.updated_at,
                created_at = x.created_at
            }).ToList();

            favorites.ForEach(x => { x.favorite = _usersService.GetDetailedV2(x.favorite_id!)!; x.user = _usersService.GetDetailedV2(x.user_id!)!; });

            return favorites;
        }

        public async Task<Favorites> GetAsync(string userid, string favorite_id)
        {
            return await _favoritesCollection.Find(x => x.user_id == userid && x.favorite_id == favorite_id).FirstOrDefaultAsync();
        }

        public (int, int) GetFavoritesCount(string userId, int year)
        {
            var countForYear = _favoritesCollection.AsQueryable().Where(x => x.user_id == userId && x.created_at.Year == year).Count();

            var allCount = _favoritesCollection.AsQueryable().Where(x => x.user_id == userId && x.created_at.Year == year).Count();

            return (countForYear, allCount);
        }
    }
}

