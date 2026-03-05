using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class EmailTemplateService
    {
        private readonly IMongoCollection<EmailTemplates> _emailTemplatesCollection;
        public EmailTemplateService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _emailTemplatesCollection = mongoDatabase.GetCollection<EmailTemplates>(
                happySugarDaddyDatabaseSettings.Value.EmailTemplatesCollectionName);
        }

        public async Task CreateAsync(EmailTemplates template) =>
         await _emailTemplatesCollection.InsertOneAsync(template);

        public async Task UpdateAsync(string id, EmailTemplates template) =>
            await _emailTemplatesCollection.ReplaceOneAsync(x => x.Id == id, template);

        public async Task<EmailTemplates?> GetAsync(string name) =>
            await _emailTemplatesCollection.Find(x => x.name == name).FirstOrDefaultAsync();

    }
}
