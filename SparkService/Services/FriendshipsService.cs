using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PayPal.v1.Orders;

namespace SparkService.Services
{
    public class FriendshipsService
    {
        private readonly IMongoCollection<Friendships> _friendshipsCollection;
        private readonly UsersService _usersService;

        public FriendshipsService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _friendshipsCollection = mongoDatabase.GetCollection<Friendships>(
                happySugarDaddyDatabaseSettings.Value.FriendshipsCollectionName);
            _usersService = usersService;
        }

        public async Task CreateAsync(Friendships friendships) =>
          await _friendshipsCollection.InsertOneAsync(friendships);

        public async Task UpdateAsync(string Id, Friendships updatedFriendships) =>
            await _friendshipsCollection.ReplaceOneAsync(x => x.Id == Id, updatedFriendships);

        public (List<FriendshipsViewModel>, int) GetMyFriends(string userId, int page, int pageSize)
        {
            var query = (from f in _friendshipsCollection.AsQueryable() where f.friend_id == userId && f.status == "Accepted" select new FriendshipsViewModel { friend_id = f.user_id }).
                Union(from p in _friendshipsCollection.AsQueryable() where p.user_id == userId && p.status == "Accepted" select new FriendshipsViewModel { friend_id = p.friend_id }).ToList();

            var friends = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            friends.ForEach(x => x.friend = _usersService.GetDetailedV2(x.friend_id)!);

            return (friends, query.Count());
        }

        public bool AreFriends(string userId, string friendId)
        {
            return _friendshipsCollection.AsQueryable().Any(x => (x.friend_id == friendId && x.user_id == userId) || (x.friend_id == userId && x.user_id == friendId));
        }

        public (int, int) GetFriendsCount(string userId, int year)
        {
            var countForYear = (from f in _friendshipsCollection.AsQueryable()
                                where f.friend_id == userId
                                && f.status == "Accepted"
                                && f.created_at.Year == year
                                select new FriendshipsViewModel
                                {
                                    friend_id = f.user_id
                                }).Union(
                from p in _friendshipsCollection.AsQueryable()
                where p.user_id == userId
                && p.status == "Accepted"
                && p.created_at.Year == year
                select new FriendshipsViewModel
                {
                    friend_id = p.friend_id
                }).Count();


            var allCount = (from f in _friendshipsCollection.AsQueryable()
                            where f.friend_id == userId
                            && f.status == "Accepted"
                            select new FriendshipsViewModel
                            {
                                friend_id = f.user_id
                            }).Union(
              from p in _friendshipsCollection.AsQueryable()
              where p.user_id == userId
              && p.status == "Accepted"
              select new FriendshipsViewModel
              {
                  friend_id = p.friend_id
              }).Count();

            return (countForYear, allCount);
        }       
    }
}
