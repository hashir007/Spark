using SparkService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Services
{
    public class NotificationsService
    {
        private readonly IMongoCollection<Notifications> _notificationsCollection;

        public NotificationsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
             happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _notificationsCollection = mongoDatabase.GetCollection<Notifications>(
                happySugarDaddyDatabaseSettings.Value.NotificationsCollectionName);
        }

        public async Task AddAsync(Notifications model) => await _notificationsCollection.InsertOneAsync(model);

        public IQueryable<Notifications> Get(string forUser) => _notificationsCollection.AsQueryable().Where(x => x.user_id == forUser);

        public async Task UpdateAsync(string id, Notifications updateNotification) => await _notificationsCollection.ReplaceOneAsync(x => x.Id == id, updateNotification);

        public async Task<List<Notifications>> GetByTypeAsync(string type, string forUser) => await _notificationsCollection.AsQueryable().Where(x => x.user_id == forUser && x.type == type).OrderByDescending(x => x.created_at).ToListAsync();

        public int GetByTypeCount(string type, string forUser) => _notificationsCollection.AsQueryable().Where(x => x.user_id == forUser && x.type == type && x.is_read == false).Count();

        public List<Tuple<string, List<Notifications>>> GetAllNotificationsForUser(string userId) => _notificationsCollection.AsQueryable().Where(x => x.user_id == userId).
            GroupBy(t => t.type, (k, c) => new Tuple<string, List<Notifications>>(k, c.ToList())).ToList();

        public async Task<Notifications> GetAsync(string id) => await _notificationsCollection.Find(x=>x.Id == id).FirstOrDefaultAsync();
    }
}
