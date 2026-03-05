using SparkService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SparkService.Services
{
    public class VenuesService
    {
        private readonly IMongoCollection<Venues> _venuesCollection;

        public VenuesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
             happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _venuesCollection = mongoDatabase.GetCollection<Venues>(
                happySugarDaddyDatabaseSettings.Value.VenuesCollectionName);
        }

        public async Task<Venues?> GetAsync(string id) => await _venuesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        public async Task AddAsync(Venues model) => await _venuesCollection.InsertOneAsync(model);
        public async Task UpdateAsync(string id, Venues updateVenues) =>
           await _venuesCollection.ReplaceOneAsync(x => x.Id == id, updateVenues);
        public async Task<List<Venues>> GetAsync() => await _venuesCollection.AsQueryable().ToListAsync();
        public List<Venues> Search(string term) => _venuesCollection.AsQueryable()
            .Where(
                 t => t.name.Contains(term) ||
                 t.manager_name.Contains(term) ||
                 t.manager_email.Contains(term) ||
                 t.manager_phone.Contains(term) ||
                 t.city.Contains(term) ||
                 t.state.Contains(term) ||
                 t.street.Contains(term) ||
                 t.street2.Contains(term) ||
                 t.zip.Contains(term) ||
                 t.country.Contains(term)
             ).ToList();

    }
}
