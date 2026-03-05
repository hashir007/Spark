using SparkService.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SparkService.Services
{
    public class ProfilesService
    {
        private readonly IMongoCollection<Profile> _profilesCollection;
        private readonly IMongoCollection<ViewsProfile> _viewsProfileCollection;
        private readonly IMongoCollection<Kisses> _kissesCollection;
        private readonly IMongoCollection<LikesDisLikesProfiles> _LikesDisLikesProfilesCollection;
        private readonly IMongoCollection<UserTraits> _userTraits;
        private readonly IMongoCollection<Interests> _interests;
        private readonly IMongoCollection<User> _user;

        public ProfilesService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings)
        {
            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _profilesCollection = mongoDatabase.GetCollection<Profile>(
                happySugarDaddyDatabaseSettings.Value.ProfileCollectionName);

            _viewsProfileCollection = mongoDatabase.GetCollection<ViewsProfile>(
                happySugarDaddyDatabaseSettings.Value.ViewsProfileCollectionName);

            _kissesCollection = mongoDatabase.GetCollection<Kisses>(
               happySugarDaddyDatabaseSettings.Value.KissesCollectionName);

            _LikesDisLikesProfilesCollection = mongoDatabase.GetCollection<LikesDisLikesProfiles>(
              happySugarDaddyDatabaseSettings.Value.LikesDisLikesProfilesCollectionName);

            _userTraits = mongoDatabase.GetCollection<UserTraits>(
              happySugarDaddyDatabaseSettings.Value.UserTraitsCollectionName);

            _interests = mongoDatabase.GetCollection<Interests>(
              happySugarDaddyDatabaseSettings.Value.InterestsCollectionName);

            _user = mongoDatabase.GetCollection<User>(
              happySugarDaddyDatabaseSettings.Value.UsersCollectionName);
        }

        public async Task CreateAsync(Profile profile) =>
           await _profilesCollection.InsertOneAsync(profile);

        public async Task UpdateAsync(string Id, Profile updatedProfile) =>
            await _profilesCollection.ReplaceOneAsync(x => x.Id == Id, updatedProfile);

        public async Task<Profile> GetByUserIdAsync(string userId) =>
          await _profilesCollection.Find(x => x.UserId == userId).FirstOrDefaultAsync();

        public List<(string, int, int, int, int)> GetProfileViewStats(string userId, int year)
        {
            var dateGrouped = new List<(string, int, int, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = year,
                month = x
            });

            int allCount = _viewsProfileCollection.AsQueryable().Where(x => x.profileId == userId).Count();

            int CountForYear = _viewsProfileCollection.AsQueryable().Where(x => x.profileId == userId && x.created_at.Year == year).Count();

            var result = (from t in _viewsProfileCollection.AsQueryable()
                          where t.profileId == userId && t.created_at.Year == year
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();

            foreach (var item in months)
            {
                dateGrouped.Add(
                    ((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                    item.year,
                    result.Any(x => x.Month == item.month) ? result.FirstOrDefault(x => x.Month == item.month)!.Total : 0,
                    CountForYear,
                    allCount
                    ));
            }

            return dateGrouped;

        }

        public List<(string, int, int, int, int)> GetProfileKissesStat(string userId, int year)
        {
            var dateGrouped = new List<(string, int, int, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = year,
                month = x
            });

            var allCount = _kissesCollection.AsQueryable().Where(x => x.kissed_id == userId).Sum(p => p.kissed_count);

            var countForUser = _kissesCollection.AsQueryable().Where(x => x.kissed_id == userId && x.created_at.Year == year).Sum(p => p.kissed_count);

            var result = (from t in _kissesCollection.AsQueryable()
                          where t.kissed_id == userId && t.created_at.Year == year
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Sum(c => c.kissed_count)
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                    item.year,
                    result.Any(x => x.Month == item.month) ? result.FirstOrDefault(x => x.Month == item.month)!.Total : 0,
                    countForUser,
                    allCount
                    ));
            }

            return dateGrouped;

        }

        public List<(string, int, int, int, int)> GetProfileLikesStat(string userId, int year)
        {
            var dateGrouped = new List<(string, int, int, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = year,
                month = x
            });

            var allCount = _LikesDisLikesProfilesCollection.AsQueryable().Where(x => x.profile_id == userId && x.isLikes).Count();

            var countForYear = _LikesDisLikesProfilesCollection.AsQueryable().Where(x => x.profile_id == userId && x.isLikes && x.created_at.Year == year).Count();

            var result = (from t in _LikesDisLikesProfilesCollection.AsQueryable()
                          where t.profile_id == userId && t.created_at.Year == year && t.isLikes
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                    item.year,
                    result.Any(x => x.Month == item.month) ? result.FirstOrDefault(x => x.Month == item.month)!.Total : 0,
                    countForYear,
                    allCount
                    ));
            }

            return dateGrouped;
        }

        public List<(string, int, int, int, int)> GetProfileDislikesStat(string userId, int year)
        {
            var dateGrouped = new List<(string, int, int, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = year,
                month = x
            });

            var allCount = _LikesDisLikesProfilesCollection.AsQueryable().Where(x => x.profile_id == userId && !x.isLikes).Count();

            var countForUser = _LikesDisLikesProfilesCollection.AsQueryable().Where(x => x.profile_id == userId && !x.isLikes && x.created_at.Year == year).Count();

            var result = (from t in _LikesDisLikesProfilesCollection.AsQueryable()
                          where t.profile_id == userId && t.created_at.Year == year && !t.isLikes
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                    item.year,
                    result.Any(x => x.Month == item.month) ? result.FirstOrDefault(x => x.Month == item.month)!.Total : 0,
                    countForUser,
                    allCount
                    ));
            }

            return dateGrouped;

        }

        public long GetProfileViewsCount(string userId) => _viewsProfileCollection.Find(x => x.userId == userId).CountDocuments();

        public (int, int) GetProfileScore(string userId)
        {
            int pending = 0;
            int progress = 0;

            if (_user.AsQueryable().Where(x => x.Id == userId).FirstOrDefault()!.is_photo_uploaded)
            {
                progress = progress + 1;
            }

            if (_userTraits.AsQueryable().Any(x => x.user_id == userId))
            {
                progress = progress + 1;
            }

            if (_interests.AsQueryable().Any(x => x.created_by == userId))
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.first_name != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.last_name != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.date_of_birth != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.gender != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.iam != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.seeking != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.height != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.race != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.martialStatus != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.annualIncome != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.bodyType != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.address != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.city != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.state != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.zip_code != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.phone_number != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.educationLevel != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.relationshipGoals != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.bio != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.profileHeadline != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.aboutYourselfInYourOwnWords != null)
            {
                progress = progress + 1;
            }

            if (_profilesCollection.AsQueryable().Where(x => x.UserId == userId).FirstOrDefault()!.describeThePersonYouAreLookingFor != null)
            {
                progress = progress + 1;
            }


            pending = 25 - progress;

            return (pending, progress);
        }

        public long GetProfileLikesCount(string userId)
        {
            return _LikesDisLikesProfilesCollection.AsQueryable().Where(x => x.profile_id == userId && x.isLikes).Count();
        }

        public long GetProfileDislikesCount(string userId)
        {
            return _LikesDisLikesProfilesCollection.AsQueryable().Where(x => x.profile_id == userId && !x.isLikes).Count();
        }

        public long GetProfileKissesCount(string userId)
        {
            return _kissesCollection.AsQueryable().Where(x => x.kissed_id == userId).Sum(p => p.kissed_count);
        }
    }
}
