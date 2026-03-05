using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class PageContentService
    {
        private readonly IMongoCollection<PageContent> _pageContentCollection;

        public PageContentService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _pageContentCollection = mongoDatabase.GetCollection<PageContent>(
                happySugarDaddyDatabaseSettings.Value.PageContentCollectionName);
        }

        public async Task CreateAsync(PageContent newPageContent) =>
           await _pageContentCollection.InsertOneAsync(newPageContent);

        public async Task UpdateAsync(string id, PageContent updatedPageContent) =>
            await _pageContentCollection.ReplaceOneAsync(x => x.Id == id, updatedPageContent);

        public async Task<List<PageContent>> GetPageNamesAsync() =>
            await _pageContentCollection.Find(_=>true).ToListAsync();
    }
}
