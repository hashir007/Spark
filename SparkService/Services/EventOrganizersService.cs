using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Services
{
    public class EventOrganizersService
    {
        private readonly IMongoCollection<EventOrganizers> _eventOrganizersCollection;

        public EventOrganizersService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
             happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _eventOrganizersCollection = mongoDatabase.GetCollection<EventOrganizers>(
                happySugarDaddyDatabaseSettings.Value.EventOrganizersCollectionName);
        }

        public async Task<EventOrganizers?> GetAsync(string id) => await _eventOrganizersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task AddAsync(EventOrganizers model) => await _eventOrganizersCollection.InsertOneAsync(model);

        public async Task UpdateAsync(string id, EventOrganizers updateVenues) =>
           await _eventOrganizersCollection.ReplaceOneAsync(x => x.Id == id, updateVenues);

        public async Task<List<EventOrganizers>> GetAsync() => await _eventOrganizersCollection.AsQueryable().ToListAsync();

        public List<EventOrganizers> Search(string term) => _eventOrganizersCollection.AsQueryable()
            .Where(
                 t => t.name.Contains(term) ||
                 t.type.Contains(term) ||
                 t.email.Contains(term)
             ).ToList();
    }
}
