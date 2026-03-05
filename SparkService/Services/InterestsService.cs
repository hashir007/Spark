using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class InterestsService
    {
        private readonly IMongoCollection<InterestCategories> _interestCategoriesCollection;
        private readonly IMongoCollection<Interests> _interestsCollection;
        private readonly UsersService _usersService;

        public InterestsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService)
        {
            var mongoClient = new MongoClient(
                 happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _interestCategoriesCollection = mongoDatabase.GetCollection<InterestCategories>(
                happySugarDaddyDatabaseSettings.Value.InterestCategoriesCollectionName);

            _interestsCollection = mongoDatabase.GetCollection<Interests>(
             happySugarDaddyDatabaseSettings.Value.InterestsCollectionName);

            _usersService = usersService;

        }

        public async Task CreateInterestCategoriesAsync(InterestCategories newBook) =>
          await _interestCategoriesCollection.InsertOneAsync(newBook);


        public async Task CreateInterestsAsync(Interests newBook) =>
          await _interestsCollection.InsertOneAsync(newBook);

        public async Task UpdateInterestsAsync(string id, Interests updatedBook) =>
           await _interestsCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveInterestsAsync(string id) =>
           await _interestsCollection.DeleteOneAsync(x => x.Id == id);

        public List<InterestCategories> GetInterestCategories()
        {
            return _interestCategoriesCollection.AsQueryable().ToList();
        }

        public List<InterestsViewModel> GetUserInterests(string userId)
        {
            var interests = _interestsCollection.AsQueryable()
                            .Where(x => x.created_by == userId)
                            .Select(p => new InterestsViewModel
                            {
                                Id = p.Id,
                                popularity = p.popularity,
                                is_active = p.is_active,
                                is_featured = p.is_featured,
                                category_id = p.category_id,
                                created_at = p.created_at,
                                modified_at = p.modified_at,
                                created_by = p.created_by,
                                modified_by = p.modified_by,
                                interest_description = p.interest_description
                            }).ToList();

            interests.ForEach(x =>
            {
                x.created_by_user = _usersService.GetDetailedV2(x.created_by!);
                x.modified_by_user = _usersService.GetDetailedV2(x.modified_by!);
                x.category = GetCategoryById(x.category_id!)!;

            });

            return interests;
        }

        public InterestCategoriesViewModel? GetCategoryById(string Id)
        {
            return _interestCategoriesCollection.AsQueryable().Where(x => x.Id == Id).Select(x => new InterestCategoriesViewModel
            {
                Id = x.Id,
                name = x.name
            }).FirstOrDefault();
        }

        public async Task<Interests> GetAsync(string id) => await _interestsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();  
    }
}
