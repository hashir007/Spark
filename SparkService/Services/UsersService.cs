using Amazon.Auth.AccessControlPolicy;
using SparkService.Extensions;
using SparkService.Helpers;
using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using PayPal.Core;
using PayPal.v1.Invoices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace SparkService.Services
{
    public class UsersService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Profile> _profilesCollection;
        private readonly IMongoCollection<Models.File> _filesCollection;
        private readonly IMongoCollection<UserRoles> _userRolesCollection;
        private readonly IMongoCollection<Roles> _rolesCollection;
        private readonly IMongoCollection<LikesDisLikesProfiles> _likesDisLikesProfilesCollection;
        private readonly IMongoCollection<Favorites> _favoritesCollection;
        private readonly IMongoCollection<Kisses> _kissesCollection;
        private readonly IMongoCollection<SparkService.Models.RefreshToken> _refreshTokenCollection;
        private readonly IMongoCollection<Subscriptions> _subscriptionsCollection;
        private readonly IMongoCollection<SubscriptionPlans> _subscriptionPlansCollection;
        private readonly IMongoCollection<SubscriptionServices> _subscriptionServicesCollection;
        private readonly IMongoCollection<SubscriptionPlanServices> _subscriptionPlanServicesCollection;
        private readonly IMongoCollection<ViewsProfile> _viewsProfileCollection;
        private readonly IMongoCollection<Photos> _photosCollection;
        private readonly IMongoCollection<BlockedList> _blockedListCollection;
        private readonly IMongoCollection<Friendships> _friendshipsCollection;
        private readonly SubscriptionService _subscriptionService;
        private readonly CompatibilityScoresService _compatibilityScoresService;
        private readonly ProfilesService _profilesService;

        public UsersService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, IHttpContextAccessor httpContextAccessor, SubscriptionService subscriptionService, CompatibilityScoresService compatibilityScoresService, ProfilesService profilesService)
        {
            _httpContextAccessor = httpContextAccessor;

            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>(
                happySugarDaddyDatabaseSettings.Value.UsersCollectionName);

            _profilesCollection = mongoDatabase.GetCollection<Profile>(
              happySugarDaddyDatabaseSettings.Value.ProfileCollectionName);

            _filesCollection = mongoDatabase.GetCollection<Models.File>(
               happySugarDaddyDatabaseSettings.Value.FileCollectionName);

            _userRolesCollection = mongoDatabase.GetCollection<UserRoles>(
               happySugarDaddyDatabaseSettings.Value.UserRolesCollectionName);

            _rolesCollection = mongoDatabase.GetCollection<Roles>(
               happySugarDaddyDatabaseSettings.Value.RolesCollectionName);

            _likesDisLikesProfilesCollection = mongoDatabase.GetCollection<LikesDisLikesProfiles>(
                happySugarDaddyDatabaseSettings.Value.LikesDisLikesProfilesCollectionName);

            _favoritesCollection = mongoDatabase.GetCollection<Favorites>(
               happySugarDaddyDatabaseSettings.Value.FavoritesCollectionName);

            _kissesCollection = mongoDatabase.GetCollection<Kisses>(
               happySugarDaddyDatabaseSettings.Value.KissesCollectionName);

            _refreshTokenCollection = mongoDatabase.GetCollection<SparkService.Models.RefreshToken>(
               happySugarDaddyDatabaseSettings.Value.RefreshTokenCollectionName);

            _subscriptionsCollection = mongoDatabase.GetCollection<Subscriptions>(
                happySugarDaddyDatabaseSettings.Value.SubscriptionsCollectionName);

            _subscriptionPlansCollection = mongoDatabase.GetCollection<SubscriptionPlans>(
               happySugarDaddyDatabaseSettings.Value.SubscriptionPlansCollectionName);

            _subscriptionServicesCollection = mongoDatabase.GetCollection<SubscriptionServices>(
              happySugarDaddyDatabaseSettings.Value.SubscriptionServicesCollectionName);

            _subscriptionPlanServicesCollection = mongoDatabase.GetCollection<SubscriptionPlanServices>(
              happySugarDaddyDatabaseSettings.Value.SubscriptionPlanServicesCollectionName);

            _photosCollection = mongoDatabase.GetCollection<Photos>(
              happySugarDaddyDatabaseSettings.Value.PhotosCollectionName);

            _viewsProfileCollection = mongoDatabase.GetCollection<ViewsProfile>(
              happySugarDaddyDatabaseSettings.Value.ViewsProfileCollectionName);

            _blockedListCollection = mongoDatabase.GetCollection<BlockedList>(
             happySugarDaddyDatabaseSettings.Value.BlockedListCollectionName);

            _friendshipsCollection = mongoDatabase.GetCollection<Friendships>(
               happySugarDaddyDatabaseSettings.Value.FriendshipsCollectionName);

            _subscriptionService = subscriptionService;

            _compatibilityScoresService = compatibilityScoresService;

            _profilesService = profilesService;

        }

        public enum UserSortOption
        {
            created_at,
            name
        }
        private IQueryable<UserViewModel> GetUserIQueryable()
        {

            var query = (from user in _usersCollection.AsQueryable()
                         join userRoles in _userRolesCollection.AsQueryable() on user.Id equals userRoles.UserId
                         join roles in _rolesCollection.AsQueryable() on userRoles.RoleId equals roles.Id into rf
                         join profile in _profilesCollection.AsQueryable() on user.Id equals profile.UserId
                         join subscriptions in _subscriptionsCollection.AsQueryable() on user.Id equals subscriptions.userId into userSubscriptions
                         from currentSubscription in userSubscriptions
                         where currentSubscription.status == SubscriptionsStatus.active.ToString().ToLower()
                         join subscriptionPlan in _subscriptionPlansCollection.AsQueryable() on currentSubscription.subscriptionPlansId equals subscriptionPlan.Id
                         join files in _filesCollection.AsQueryable() on profile.photo equals files.Id into photos
                         from photoFile in photos.DefaultIfEmpty()
                         select new UserViewModel
                         {
                             id = user.Id,
                             email_address = user.email_address,
                             is_active = user.is_active,
                             language = user.language,
                             timezone = user.timezone,
                             username = user.username,
                             is_email_verified = user.is_email_verified,
                             is_photo_uploaded = user.is_photo_uploaded,
                             last_login = user.last_login,
                             created_at = user.created_at,
                             updated_at = user.updated_at,
                             profile = new ProfileViewModel
                             {
                                 aboutYourselfInYourOwnWords = profile.aboutYourselfInYourOwnWords,
                                 address = profile.address,
                                 address2 = profile.address2,
                                 bio = profile.bio,
                                 bodyType = profile.bodyType,
                                 city = profile.city,
                                 country = profile.country,
                                 date_of_birth = profile.date_of_birth,
                                 age = !profile.date_of_birth.HasValue ? null : ((DateTime.Now.Month < profile.date_of_birth!.Value.Month || (DateTime.Now.Month == profile.date_of_birth!.Value.Month && DateTime.Now.Day < profile.date_of_birth!.Value.Day)) ?
                                                               (DateTime.Now.Date.Year - profile.date_of_birth!.Value.Date.Year) - 1 : (DateTime.Now.Date.Year - profile.date_of_birth!.Value.Date.Year)),
                                 describeThePersonYouAreLookingFor = profile.describeThePersonYouAreLookingFor,
                                 first_name = profile.first_name,
                                 last_name = profile.last_name,
                                 gender = profile.gender.ToString(),
                                 iam = profile.iam.ToString(),
                                 martialStatus = profile.martialStatus,
                                 phone_number = profile.phone_number,
                                 photo = new FileViewModel
                                 {
                                     id = photoFile.Id,
                                     orignalName = photoFile.originalName,
                                     type = photoFile.type,
                                     name = photoFile.name,
                                     size = photoFile.size,
                                     original = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (photoFile.query_original)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (photoFile.query_original))),
                                     d480x320 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (photoFile.query_480x320)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (photoFile.query_480x320))),
                                     d300x300 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (photoFile.query_300x300)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (photoFile.query_300x300))),
                                     d100x100 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (photoFile.query_100x100)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (photoFile.query_100x100))),
                                     d16x16 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (photoFile.query_16x16)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (photoFile.query_16x16))),
                                     d32x32 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (photoFile.query_32x32)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (photoFile.query_32x32)))
                                 },
                                 seeking = profile.seeking.ToString(),
                                 educationLevel = profile.educationLevel,
                                 relationshipGoals = profile.relationshipGoals,
                                 race = profile.race,
                                 state = profile.state,
                                 zip_code = profile.zip_code,
                                 height = profile.height,
                                 profileHeadline = profile.profileHeadline,
                                 annualIncome = profile.annualIncome
                             },
                             roles = rf.ToList(),
                             Subscription = new SubscriptionsViewModel
                             {
                                 Id = currentSubscription.Id,
                                 end_date = currentSubscription.end_date,
                                 start_date = currentSubscription.start_date,
                                 status = currentSubscription.status,
                                 Plan = new SubscriptionPlansViewModel
                                 {
                                     id = subscriptionPlan.Id,
                                     colour = subscriptionPlan.colour,
                                     name = subscriptionPlan.name,
                                     description = subscriptionPlan.description,
                                     descriptionHTML = subscriptionPlan.descriptionHTML,
                                     type = subscriptionPlan.type,
                                     order = subscriptionPlan.order,
                                     price = subscriptionPlan.price,
                                     paypal_plan_id = subscriptionPlan.paypal_plan_id,
                                     status = subscriptionPlan.status,
                                     created_at = subscriptionPlan.created_at,
                                     updated_at = subscriptionPlan.updated_at,
                                     storage = subscriptionPlan.storage,

                                 },
                             },
                         });

            return query;
        }

        #region Fetch User

        public UserViewModel? GetDetailed(string id)
        {
            return GetUserIQueryable().FirstOrDefault(x => x.id == id);
        }
        public UserViewModelV2? GetDetailedV2(string id)
        {
            return GetUserIQueryable().ToUsersV2().FirstOrDefault(x => x.id == id);
        }
        public UserViewModelV3? GetDetailedV3(string id)
        {
            return GetUserIQueryable().ToUsersV3().FirstOrDefault(x => x.id == id);
        }
        public UserViewModelV4? GetDetailedV4(string id)
        {
            return GetUserIQueryable().ToUsersV4().FirstOrDefault(x => x.id == id);
        }
        public MemberViewModel? GetMemberById(string id)
        {
            return GetUserIQueryable().Select(x => new MemberViewModel
            {
                id = x.id,
                is_active = x.is_active,
                language = x.language,
                profile = new MemberProfileViewModel
                {
                    aboutYourselfInYourOwnWords = x.profile.aboutYourselfInYourOwnWords,
                    age = x.profile.age,
                    annualIncome = x.profile.annualIncome,
                    bio = x.profile.bio,
                    bodyType = x.profile.bodyType,
                    describeThePersonYouAreLookingFor = x.profile.describeThePersonYouAreLookingFor,
                    educationLevel = x.profile.educationLevel,
                    gender = x.profile.gender,
                    height = x.profile.height,
                    iam = x.profile.iam,
                    martialStatus = x.profile.martialStatus,
                    photo = x.profile.photo,
                    profileHeadline = x.profile.profileHeadline,
                    race = x.profile.race,
                    relationshipGoals = x.profile.relationshipGoals,
                    seeking = x.profile.seeking,
                    state = x.profile.state,
                    city = x.profile.city,
                    country = x.profile.country,
                    zip_code = x.profile.zip_code
                },
                timezone = x.timezone,
                username = x.username,
                lastLogin = x.last_login,
                Subscription = new SubscriptionV3ViewModel()
                {
                    Plan = x.Subscription.Plan,
                    status = x.Subscription.status
                },
                galleyPhotoCount = GetUserPhotosCount(id)
            }).FirstOrDefault(x => x.id == id);
        }

        #endregion

        private long GetUserPhotosCount(string id)
        {
            return _photosCollection.Find(x => x.userId == id).CountDocuments();
        }

        public async Task<User?> GetAsync(string id) => await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        public User? GetAsync(string username, string password) => _usersCollection.AsQueryable().Where(x => x.username == username && x.password == password).FirstOrDefault();
        public async Task<User?> GetByUsernameAsync(string username) => await _usersCollection.Find(x => x.username == username).FirstOrDefaultAsync();
        public async Task<bool> ValidateUsernameExistsAsync(string username) => await _usersCollection.AsQueryable().AnyAsync(x => x.username.ToLower() == username.ToLower());
        public async Task<bool> ValidateEmailExistsAsync(string email) => await _usersCollection.AsQueryable().AnyAsync(x => x.email_address.ToLower() == email.ToLower());
        public async Task<User?> GetByEmailAsync(string email) => await _usersCollection.Find(x => x.email_address == email).FirstOrDefaultAsync();

        private bool AreFriends(string userId, string friendId) { return _friendshipsCollection.AsQueryable().Any(x => (x.friend_id == friendId && x.user_id == userId) || (x.friend_id == userId && x.user_id == friendId)); }

        public (List<MemberViewModel>, int) Filter(string userId, string iam, string seeking, string ageFrom, string ageTo, string[] race, string[] gender, string[] educationLevel, string heightFrom, string heightTo, string martialStatus, string[] income, string[] bodyType, string[] country, int page, int pageSize)
        {
            var query = GetUserIQueryable().ApplyRoleFilter().AsQueryable();

            if (!string.IsNullOrEmpty(iam) && !string.IsNullOrEmpty(seeking))
            {
                query = query.Where(x => x.profile.iam.ToString() == seeking && x.profile.seeking.ToString() == iam);
            }

            if (!string.IsNullOrEmpty(ageFrom))
            {
                query = query.Where(x => x.profile.age >= Convert.ToInt32(ageFrom));
            }

            if (!string.IsNullOrEmpty(ageTo))
            {
                query = query.Where(x => x.profile.age <= Convert.ToInt32(ageTo));
            }

            if (race != null && race.Length > 0)
            {
                query = query.Where(x => race.Contains(x.profile.race));
            }

            if (gender != null && gender.Length > 0)
            {
                query = query.Where(x => gender.Contains(x.profile.gender));
            }

            if (educationLevel != null && educationLevel.Length > 0)
            {
                query = query.Where(x => educationLevel.Contains(x.profile.educationLevel));
            }

            if (!string.IsNullOrEmpty(heightFrom))
            {
                query = query.Where(x => (x.profile.height_in_inches) == SparkService.Helpers.CalculationsHelpers.ToInches(heightFrom));
            }

            if (!string.IsNullOrEmpty(heightTo))
            {
                query = query.Where(x => (x.profile.height_in_inches) == SparkService.Helpers.CalculationsHelpers.ToInches(heightTo));
            }

            if (!string.IsNullOrEmpty(martialStatus))
            {
                query = query.Where(x => x.profile.martialStatus == martialStatus);
            }

            if (income != null && income.Length > 0)
            {
                query = query.Where(x => income.Contains(x.profile.annualIncome));
            }

            if (bodyType != null && bodyType.Length > 0)
            {
                query = query.Where(x => bodyType.Contains(x.profile.bodyType));
            }

            if (country != null && country.Length > 0)
            {
                query = query.Where(x => country.Contains(x.profile.country));
            }

            query = _compatibilityScoresService.ApplyCompatibilityScoreSortingV1(userId, query);

            query = query.ApplySorting(UserSortOption.created_at);

            var resultCount = query.Count();

            query = query.ApplyPagination(page, pageSize);

            var formattedQuery = query.Select(x => new MemberViewModel
            {
                id = x.id,
                is_active = x.is_active,
                language = x.language,
                lastLogin = x.last_login,
                profile = new MemberProfileViewModel
                {
                    aboutYourselfInYourOwnWords = x.profile.aboutYourselfInYourOwnWords,
                    age = x.profile.age,
                    annualIncome = x.profile.annualIncome,
                    bio = x.profile.bio,
                    bodyType = x.profile.bodyType,
                    describeThePersonYouAreLookingFor = x.profile.describeThePersonYouAreLookingFor,
                    educationLevel = x.profile.educationLevel,
                    gender = x.profile.gender,
                    height = x.profile.height,
                    iam = x.profile.iam,
                    martialStatus = x.profile.martialStatus,
                    photo = x.profile.photo,
                    profileHeadline = x.profile.profileHeadline,
                    race = x.profile.race,
                    relationshipGoals = x.profile.relationshipGoals,
                    seeking = x.profile.seeking,
                    state = x.profile.state,
                    city = x.profile.city,
                    country = x.profile.country,
                    zip_code = x.profile.zip_code
                },
                timezone = x.timezone,
                username = x.username,
                Subscription = new SubscriptionV3ViewModel()
                {
                    Plan = x.Subscription.Plan,
                    status = x.Subscription.status
                }
            }).AsQueryable();

            var result = formattedQuery.ToList();

            foreach (var user in result)
            {
                var isLikes = Task.Run(async () => await GetLikeForUserAsync(userId!, user.id!)).Result;

                if (isLikes is null)
                {
                    user.isLike = null;
                }
                else
                {
                    user.isLike = isLikes.isLikes;
                }

                user.total_like_count = _profilesService.GetProfileLikesCount(user.id!);
                user.total_dislike_count = _profilesService.GetProfileDislikesCount(user.id!);

                var isFavorite = Task.Run(async () => await GetFavoritesAsync(userId!, user.id!)).Result;

                if (isFavorite is not null)
                {
                    user.isFavorite = true;
                }

                var isKissed = Task.Run(async () => await GetKissesAsync(userId!, user.id!)).Result;

                if (isKissed is not null)
                {
                    user.iskissed = true;
                }

                user.total_kiss_count = _profilesService.GetProfileKissesCount(user.id!);

                var isBlocked = Task.Run(async () => await GetBlockedListForMember(userId!, user.id!)).Result;

                if (isBlocked is not null)
                {
                    user.isBlocked = true;
                }

                user.isFriend = AreFriends(userId, user.id!);
            }

            return (result, resultCount);
        }
        public List<MemberViewModel> RecentMembers100(string iam)
        {
            var query = GetUserIQueryable().ApplyRoleFilter().ApplySorting(UserSortOption.created_at).AsQueryable();

            query = query.Where(x => x.profile.iam.ToString() == iam);

            query = query.ApplyPagination(1, 100);

            var result = query.Select(x => new MemberViewModel
            {
                id = x.id,
                is_active = x.is_active,
                language = x.language,
                profile = new MemberProfileViewModel
                {
                    aboutYourselfInYourOwnWords = x.profile.aboutYourselfInYourOwnWords,
                    age = x.profile.age,
                    annualIncome = x.profile.annualIncome,
                    bio = x.profile.bio,
                    bodyType = x.profile.bodyType,
                    describeThePersonYouAreLookingFor = x.profile.describeThePersonYouAreLookingFor,
                    educationLevel = x.profile.educationLevel,
                    gender = x.profile.gender,
                    height = x.profile.height,
                    iam = x.profile.iam,
                    martialStatus = x.profile.martialStatus,
                    photo = x.profile.photo,
                    profileHeadline = x.profile.profileHeadline,
                    race = x.profile.race,
                    relationshipGoals = x.profile.relationshipGoals,
                    seeking = x.profile.seeking,
                    state = x.profile.state,
                    city = x.profile.city,
                    country = x.profile.country,
                    zip_code = x.profile.zip_code
                },
                timezone = x.timezone,
                username = x.username,
                Subscription = new SubscriptionV3ViewModel()
                {
                    Plan = x.Subscription.Plan,
                    status = x.Subscription.status
                }
            }).ToList();

            return result;
        }
        public (List<UserViewModel>, int) GetAllUsersPaged(int page, int pageSize, string? search)
        {
            var query = GetUserIQueryable().ApplyRoleFilter().ApplySorting(UserSortOption.created_at);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                  x.username.Contains(search, System.StringComparison.CurrentCultureIgnoreCase) ||
                  x.email_address.Contains(search, System.StringComparison.CurrentCultureIgnoreCase) ||
                  x.profile.first_name.Contains(search, System.StringComparison.CurrentCultureIgnoreCase) ||
                  x.profile.last_name.Contains(search, System.StringComparison.CurrentCultureIgnoreCase)
                );
            }

            var result = query.ApplyPagination(page, pageSize).ToList();

            return (result, query.Count());
        }
        public List<MemberViewModel> GetAllUsers(string? search, int pageSize)
        {
            var query = GetUserIQueryable().ApplyRoleFilter().ApplySorting(UserSortOption.created_at);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                                x.username.Contains(search, System.StringComparison.CurrentCultureIgnoreCase) ||
                                x.email_address.Contains(search, System.StringComparison.CurrentCultureIgnoreCase) ||
                                x.profile.first_name.Contains(search, System.StringComparison.CurrentCultureIgnoreCase) ||
                                x.profile.last_name.Contains(search, System.StringComparison.CurrentCultureIgnoreCase)
                            );

            }

            var result = query.ApplyPagination(1, pageSize).Select(x => new MemberViewModel
            {
                id = x.id,
                is_active = x.is_active,
                language = x.language,
                profile = new MemberProfileViewModel
                {
                    aboutYourselfInYourOwnWords = x.profile.aboutYourselfInYourOwnWords,
                    age = x.profile.age,
                    annualIncome = x.profile.annualIncome,
                    bio = x.profile.bio,
                    bodyType = x.profile.bodyType,
                    describeThePersonYouAreLookingFor = x.profile.describeThePersonYouAreLookingFor,
                    educationLevel = x.profile.educationLevel,
                    gender = x.profile.gender,
                    height = x.profile.height,
                    iam = x.profile.iam,
                    martialStatus = x.profile.martialStatus,
                    photo = x.profile.photo,
                    profileHeadline = x.profile.profileHeadline,
                    race = x.profile.race,
                    relationshipGoals = x.profile.relationshipGoals,
                    seeking = x.profile.seeking,
                    state = x.profile.state,
                    city = x.profile.city,
                    country = x.profile.country,
                    zip_code = x.profile.zip_code
                },
                timezone = x.timezone,
                username = x.username,
                Subscription = new SubscriptionV3ViewModel()
                {
                    Plan = x.Subscription.Plan,
                    status = x.Subscription.status
                }
            }).ToList();

            return result;
        }

        public async Task<List<MemberViewModel>> SimilarMembers(string userId, string memberId)
        {
            List<MemberViewModel> members = new List<MemberViewModel>();

            var compatibilityScore = await _compatibilityScoresService.GetAsync(userId, memberId);

            if (compatibilityScore is null)
            {
                var query = GetUserIQueryable().ApplyRoleFilter().ApplySorting(UserSortOption.created_at).AsQueryable();

                query = query.ApplyPagination(1, 4);

                var formattedQuery = query.Select(x => new MemberViewModel
                {
                    id = x.id,
                    is_active = x.is_active,
                    language = x.language,
                    profile = new MemberProfileViewModel
                    {
                        aboutYourselfInYourOwnWords = x.profile.aboutYourselfInYourOwnWords,
                        age = x.profile.age,
                        annualIncome = x.profile.annualIncome,
                        bio = x.profile.bio,
                        bodyType = x.profile.bodyType,
                        describeThePersonYouAreLookingFor = x.profile.describeThePersonYouAreLookingFor,
                        educationLevel = x.profile.educationLevel,
                        gender = x.profile.gender,
                        height = x.profile.height,
                        iam = x.profile.iam,
                        martialStatus = x.profile.martialStatus,
                        photo = x.profile.photo,
                        profileHeadline = x.profile.profileHeadline,
                        race = x.profile.race,
                        relationshipGoals = x.profile.relationshipGoals,
                        seeking = x.profile.seeking,
                        state = x.profile.state,
                        city = x.profile.city,
                        country = x.profile.country,
                        zip_code = x.profile.zip_code
                    },
                    timezone = x.timezone,
                    username = x.username,
                    Subscription = new SubscriptionV3ViewModel()
                    {
                        Plan = x.Subscription.Plan,
                        status = x.Subscription.status
                    }
                }).AsQueryable();

                members = formattedQuery.ToList();
            }
            else
            {
                var campatibilityScores = _compatibilityScoresService.GetMembersFromSpecificCompatibilityOnwards(userId, compatibilityScore.score, 1, 4);

                if (campatibilityScores.Count > 0)
                {
                    var membersQueryable = GetUserIQueryable().ApplyRoleFilter();

                    members = (from comp in campatibilityScores
                               join mem in membersQueryable on comp.other_user_id equals mem.id
                                into gMem
                               from gMember in gMem.DefaultIfEmpty()
                               select new MemberViewModel
                               {
                                   id = gMember.id,
                                   is_active = gMember.is_active,
                                   language = gMember.language,
                                   profile = new MemberProfileViewModel
                                   {
                                       aboutYourselfInYourOwnWords = gMember.profile.aboutYourselfInYourOwnWords,
                                       age = gMember.profile.age,
                                       annualIncome = gMember.profile.annualIncome,
                                       bio = gMember.profile.bio,
                                       bodyType = gMember.profile.bodyType,
                                       describeThePersonYouAreLookingFor = gMember.profile.describeThePersonYouAreLookingFor,
                                       educationLevel = gMember.profile.educationLevel,
                                       gender = gMember.profile.gender,
                                       height = gMember.profile.height,
                                       iam = gMember.profile.iam,
                                       martialStatus = gMember.profile.martialStatus,
                                       photo = gMember.profile.photo,
                                       profileHeadline = gMember.profile.profileHeadline,
                                       race = gMember.profile.race,
                                       relationshipGoals = gMember.profile.relationshipGoals,
                                       seeking = gMember.profile.seeking,
                                       state = gMember.profile.state,
                                       city = gMember.profile.city,
                                       country = gMember.profile.country,
                                       zip_code = gMember.profile.zip_code
                                   },
                                   timezone = gMember.timezone,
                                   username = gMember.username,
                                   Subscription = new SubscriptionV3ViewModel()
                                   {
                                       Plan = gMember.Subscription.Plan,
                                       status = gMember.Subscription.status
                                   }
                               }
                               ).ToList();
                }

            }

            return members;
        }


        public async Task CreateAsync(User newBook) =>
           await _usersCollection.InsertOneAsync(newBook);

        public async Task UpdateAsync(string id, User updatedBook) =>
            await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveAsync(string id) =>
            await _usersCollection.DeleteOneAsync(x => x.Id == id);

        public async Task<LikesDisLikesProfiles> GetLikeForUserAsync(string userid, string profile_id)
        {
            return await _likesDisLikesProfilesCollection.Find(x => x.user_id == userid && x.profile_id == profile_id).FirstOrDefaultAsync();
        }


        public async Task<Favorites> GetFavoritesAsync(string userid, string favorite_id)
        {
            return await _favoritesCollection.Find(x => x.user_id == userid && x.favorite_id == favorite_id).FirstOrDefaultAsync();
        }

        public async Task<Kisses> GetKissesAsync(string userid, string kissed_id)
        {
            return await _kissesCollection.Find(x => x.user_id == userid && x.kissed_id == kissed_id).FirstOrDefaultAsync();
        }

        public List<User> SearchByUsernameAsync(string username) => _usersCollection.AsQueryable().Where(x => x.username.Contains(username, System.StringComparison.CurrentCultureIgnoreCase)).ToList();

        public List<string> GetAllUsersIds()
        {
            return _usersCollection.AsQueryable().Select(x => x.Id!).ToList<string>();
        }

        #region ViewsProfile

        public async Task CreateViewProfileAsync(ViewsProfile newBook) =>
            await _viewsProfileCollection.InsertOneAsync(newBook);

        public async Task<ViewsProfile?> GetViewProfileAsync(string profileId) =>
            await _viewsProfileCollection.Find(x => x.profileId == profileId).FirstOrDefaultAsync();

        public async Task<bool> DoesViewFromUserExistsForProfile(string userId, string profileId) =>
            await _viewsProfileCollection.Find(x => x.profileId == profileId && x.userId == userId).AnyAsync();

        public async Task<long> GetViewsProfileCount(string profileId) =>
            await _viewsProfileCollection.Find(x => x.profileId == profileId).CountDocumentsAsync();

        #endregion

        #region BlockedList

        public async Task CreateBlockListAsync(BlockedList blockedList) =>
            await _blockedListCollection.InsertOneAsync(blockedList);

        public async Task<List<BlockedList>> GetBlockedListAsync(string userId) =>
           await _blockedListCollection.Find(x => x.blocked_by == userId).ToListAsync();

        public (List<BlockedListViewModel>, int) GetBlockedListMembers(int page, int pageSize, string userId)
        {
            var query = _blockedListCollection.AsQueryable().Where(x => x.blocked_by == userId);

            var blockedItems = query.Skip((page - 1) * pageSize).Take(pageSize).
                  Select(p => new BlockedListViewModel
                  {
                      Id = p.Id,
                      member_id = p.member_id,
                      blocked_by = p.blocked_by,
                      created_at = p.created_at
                  }).ToList();

            blockedItems.ForEach(x => { x.Member = GetDetailedV3(x.member_id); x.User = GetDetailedV4(x.blocked_by); });

            return (blockedItems, query.Count());
        }

        public async Task<BlockedList> GetBlockedListForMember(string userId, string memberId) =>
            await _blockedListCollection.Find(x => x.member_id == memberId && x.blocked_by == userId).FirstOrDefaultAsync();

        public bool CheckIfBlocked(string userId, string memberId) => _blockedListCollection.AsQueryable().Any(x => (x.blocked_by == userId && x.member_id == memberId) || (x.member_id == userId && x.blocked_by == memberId));

        public async Task<DeleteResult> BlockedListMemberRemove(string userId, string memberId) => await _blockedListCollection.DeleteOneAsync(x => x.member_id == memberId && x.blocked_by == userId);

        public (int, int) BlockedListCount(string userId, int year)
        {
            var countForYear = _blockedListCollection.AsQueryable().Where(x => x.blocked_by == userId && x.created_at.Year == year).Count();

            var allCount = _blockedListCollection.AsQueryable().Where(x => x.blocked_by == userId).Count();

            return (countForYear, allCount);
        }

        #endregion       

        #region JWT Token Management

        public bool IsRefreshTokenUnique(string refreshToken)
        {
            return _refreshTokenCollection.AsQueryable().Any(x => x.token == refreshToken);
        }

        public async Task AddRefreshToken(SparkService.Models.RefreshToken refreshToken) =>
            await _refreshTokenCollection.InsertOneAsync(refreshToken);

        public void RemoveExpiredRefreshToken(string userId, int RefreshTokenExpiryTimeDays)
        {
            var refreshTokens = _refreshTokenCollection.AsQueryable().Where(x => x.UserId == userId && !x.IsActive && x.created_at.AddDays(RefreshTokenExpiryTimeDays) <= DateTime.UtcNow).ToList();

            foreach (var token in refreshTokens)
            {
                _refreshTokenCollection.DeleteOne(x => x.Id == token.Id);
            }
        }

        public SparkService.Models.RefreshToken? GetRefreshToken(string refreshToken)
        {
            return _refreshTokenCollection.Find(x => x.token == refreshToken).FirstOrDefault();
        }

        public User? GetUserByRefreshToken(string refreshToken)
        {
            var userToken = GetRefreshToken(refreshToken);
            if (userToken is null)
            {
                return null;
            }

            return _usersCollection.Find(x => x.Id == userToken.UserId).FirstOrDefault();
        }

        public void RevokeDescendantRefreshTokens(SparkService.Models.RefreshToken refreshToken, string ipAddress, string reason)
        {
            // recursively traverse the refresh token chain and ensure all descendants are revoked
            if (!string.IsNullOrEmpty(refreshToken.replaced_by_token))
            {
                var childToken = _refreshTokenCollection.AsQueryable().SingleOrDefault(x => x.token == refreshToken.replaced_by_token);
                if (childToken!.IsActive)
                    RevokeRefreshToken(childToken, ipAddress, reason);
                else
                    RevokeDescendantRefreshTokens(childToken, ipAddress, reason);
            }
        }

        public void RevokeRefreshToken(SparkService.Models.RefreshToken token, string ipAddress, string reason = null, string replacedByToken = null)
        {
            token.revoked = DateTime.UtcNow;
            token.revoked_by_ip = ipAddress;
            token.reason_revoked = reason;
            token.replaced_by_token = replacedByToken;

            _refreshTokenCollection.ReplaceOne(x => x.Id == token.Id, token);
        }

        #endregion



    }
}

