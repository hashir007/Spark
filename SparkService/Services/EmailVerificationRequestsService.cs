using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class EmailVerificationRequestsService
    {
        private readonly IMongoCollection<EmailVerificationRequests> _emailVerificationRequestsCollection;
        public EmailVerificationRequestsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _emailVerificationRequestsCollection = mongoDatabase.GetCollection<EmailVerificationRequests>(
                happySugarDaddyDatabaseSettings.Value.EmailVerificationRequestsCollectionName);
        }

        public async Task CreateAsync(EmailVerificationRequests request) =>
         await _emailVerificationRequestsCollection.InsertOneAsync(request);

        public async Task UpdateAsync(string id, EmailVerificationRequests request) =>
            await _emailVerificationRequestsCollection.ReplaceOneAsync(x => x.Id == id, request);

        public async Task<EmailVerificationRequests?> GetAsync(string token) =>
            await _emailVerificationRequestsCollection.Find(x => x.token == token).FirstOrDefaultAsync();
    }
}
