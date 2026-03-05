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
    public class EventsService
    {
        private readonly IMongoCollection<Events> _eventsCollection;

        public EventsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
             happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _eventsCollection = mongoDatabase.GetCollection<Events>(
                happySugarDaddyDatabaseSettings.Value.EventsCollectionName);
        }

        public async Task<Events?> GetAsync(string id) => await _eventsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task AddAsync(Events model) => await _eventsCollection.InsertOneAsync(model);

        public async Task UpdateAsync(string id, Events updateEvent) =>
           await _eventsCollection.ReplaceOneAsync(x => x.Id == id, updateEvent);

        public async Task<List<Events>> GetAsync() => await _eventsCollection.AsQueryable().ToListAsync();

        public List<Events> Search(string term) => _eventsCollection.AsQueryable()
            .Where(
                 t => t.name.Contains(term) ||
                 t.description.Contains(term) ||
                 t.type.Contains(term) ||
                 t.status.Contains(term)
             ).ToList();

    }
}
