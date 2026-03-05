using SparkService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Services
{
    public class ForgotPasswordRequestsService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoCollection<ForgotPasswordRequests> _forgetPasswordRequests;
        private readonly UsersService _usersService;

        public ForgotPasswordRequestsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, IHttpContextAccessor httpContextAccessor, UsersService usersService)
        {
            _httpContextAccessor = httpContextAccessor;

            _usersService = usersService;

            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _forgetPasswordRequests = mongoDatabase.GetCollection<ForgotPasswordRequests>(
             happySugarDaddyDatabaseSettings.Value.ForgotPasswordRequestsCollectionName);
        }

        public async Task CreateAsync(ForgotPasswordRequests requests) =>
           await _forgetPasswordRequests.InsertOneAsync(requests);

        public async Task<List<ForgotPasswordRequests>> GetAsync() =>
            await _forgetPasswordRequests.Find(_ => true).ToListAsync();

        public async Task<List<ForgotPasswordRequests>> GetByEmailAsync(string email) =>
            await _forgetPasswordRequests.Find(x => x.email == email).ToListAsync();

        public async Task UpdateAsync(string Id, ForgotPasswordRequests requests) =>
           await _forgetPasswordRequests.ReplaceOneAsync(x => x.Id == Id, requests);

        public async Task RemoveAsync(string id) =>
          await _forgetPasswordRequests.DeleteOneAsync(x => x.Id == id);

        public async Task<ForgotPasswordRequests?> GetByTokenAsync(string token) =>
           await _forgetPasswordRequests.Find(x => x.token == token).FirstOrDefaultAsync();
    }
}
