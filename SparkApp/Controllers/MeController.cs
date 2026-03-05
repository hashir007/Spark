using SparkApp.APIModel.Profile;
using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client.Core.DependencyInjection.Services.Interfaces;
using System.Security.Claims;
using Newtonsoft.Json;
using SparkApp.APIModel.User;
using SparkApp.APIModel.Member;
using Asp.Versioning;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using SparkApp.Extensions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SparkApp.Services;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Microsoft.Extensions.Options;


namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/me")]
    public class MeController : Controller
    {
        private readonly ILogger<MeController> _logger;
        private readonly AppSettings _config;
        private readonly UsersService _usersService;
        private readonly ProfilesService _profilesService;
        private readonly EncryptionService _encryptionService;
        private readonly TraitsService _traitsService;
        private readonly InterestsService _interestsService;
        private readonly IProducingService _rabitMQProducerService;
        private readonly PhotosService _photosService;
        private readonly UserRolesService _userRolesService;
        private readonly RolesService _rolesService;
        private readonly MailService _mailService;
        private readonly EmailVerificationRequestsService _emailVerificationRequestsService;
        private readonly NotificationsService _notificationsService;
        private readonly SubscriptionService _subscriptionService;
        private readonly KissesService _kissesService;
        private readonly LikesDisLikesProfilesService _likesDisLikesProfilesService;
        private readonly FriendshipsService _friendshipsService;
        private readonly FavoritesService _favoritesService;
        private readonly UserNotificationService _userNotificationService;


        public MeController(ILogger<MeController> logger, UsersService usersService, ProfilesService profilesService, UserRolesService userRolesService, RolesService rolesService, EncryptionService encryptionService, TraitsService traitsService, InterestsService interestsService, IProducingService rabitMQProducerService, PhotosService photosService, MailService mailService, EmailVerificationRequestsService emailVerificationRequestsService, NotificationsService notificationsService, SubscriptionService subscriptionService, KissesService kissesService, LikesDisLikesProfilesService likesDisLikesProfilesService, FriendshipsService friendshipsService, FavoritesService favoritesService, UserNotificationService userNotificationService, IOptions<AppSettings> config)
        => (_logger, _usersService, _profilesService, _userRolesService, _rolesService, _encryptionService, _traitsService, _interestsService, _rabitMQProducerService, _photosService, _mailService, _emailVerificationRequestsService, _notificationsService, _subscriptionService, _kissesService, _likesDisLikesProfilesService, _friendshipsService, _favoritesService, _userNotificationService, _config) = (logger, usersService, profilesService, userRolesService, rolesService, encryptionService, traitsService, interestsService, rabitMQProducerService, photosService, mailService, emailVerificationRequestsService, notificationsService, subscriptionService, kissesService, likesDisLikesProfilesService, friendshipsService, favoritesService, userNotificationService, config.Value);


        [MapToApiVersion(1)]
        [HttpGet("profile")]
        public ActionResult<ResponseModel<object>> Profile()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.Profile Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var member = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = member;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("notifications/{type}")]
        public async Task<ActionResult<ResponseModel<List<NotificationsViewModel>>>> GetNotifications([FromRoute][BindRequired] string type)
        {
            ResponseModel<List<NotificationsViewModel>> responseModel = new ResponseModel<List<NotificationsViewModel>>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.GetNotifications Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var notifications = await _notificationsService.GetByTypeAsync(type, userId!);

            var list = new List<NotificationsViewModel>();

            foreach (var notification in notifications)
            {

                list.Add(new NotificationsViewModel().ToNotificationsViewModel(notification));
            }



            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = list;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("notifications/{criteria?}/{page:int}/{pageSize:int}")]
        public ActionResult<ResponseModel<object>> GetNotificationAll([FromRoute][Optional] string criteria, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.GetNotificationAll Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            var notification = _notificationsService.Get(userId!);

            if (!string.IsNullOrEmpty(criteria))
            {
                JArray filters = JArray.Parse(criteria);

                foreach (JToken filter in filters)
                {
                    if (((JObject)filter).Properties().Select(p => p.Name).FirstOrDefault() == "type")
                    {
                        string? value = ((JObject)filter).GetValue("type")?.ToString();

                    }
                }
            }

            int total = notification.Count();
            var items = notification.OrderByDescending(x => x.created_at).Skip(((page - 1) * pageSize)).Take(pageSize).ToList();

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = total,
                Items = items,
                page = page,
                pageSize = pageSize
            }; ;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("notifications/summary")]
        public ActionResult<ResponseModel<object>> GetNotificationSummary()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.GetNotificationSummary Error = userId = {userId} not found.");
                throw new Exception("userId not found");
            }


            var summary = new List<object>();


            foreach (var item in Enum.GetValues(typeof(NotificationType)))
            {
                summary.Add(new Tuple<string, int>(((NotificationType)item).GetDescription(), _notificationsService.GetByTypeCount(((NotificationType)item).GetDescription(), userId!)));
            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = summary;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("subscription")]
        public ActionResult<ResponseModel<object>> GetSubscription()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.GetSubscription Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var subscription = _subscriptionService.GetSubscription(userId!);


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = subscription;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("profile-summary")]
        public ActionResult<ResponseModel<object>> GetProfileSummary()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.GetProfileSummary Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profileScore = _profilesService.GetProfileScore(userId);

            var views = _profilesService.GetProfileViewStats(userId, DateTime.Now.Year);

            var likes = _profilesService.GetProfileLikesStat(userId, DateTime.Now.Year);

            var dislikes = _profilesService.GetProfileDislikesStat(userId, DateTime.Now.Year);

            var kisses = _profilesService.GetProfileKissesStat(userId, DateTime.Now.Year);

            var photo = _photosService.GetPhotoUploadStats(userId, DateTime.Now.Year);

            var friends = _friendshipsService.GetFriendsCount(userId, DateTime.Now.Year);

            var favorites = _favoritesService.GetFavoritesCount(userId, DateTime.Now.Year);

            var blockedList = _usersService.BlockedListCount(userId, DateTime.Now.Year);

            var likesStats = new List<object[]>();

            likesStats.Add(["Month", "Likes"]);

            for (int i = 1; i < 13; i++)
            {
                likesStats.Add([(new DateTime(DateTime.Now.Year, i, 1)).ToString("MMM"), likes[i - 1].Item3]);
            }

            var dislikesStats = new List<object[]>();

            dislikesStats.Add(["Month", "Dislikes"]);

            for (int i = 1; i < 13; i++)
            {
                dislikesStats.Add([(new DateTime(DateTime.Now.Year, i, 1)).ToString("MMM"), dislikes[i - 1].Item3]);
            }

            var profileViewStats = new List<object[]>();

            profileViewStats.Add(["Month", "Profile"]);

            for (int i = 1; i < 13; i++)
            {
                profileViewStats.Add([(new DateTime(DateTime.Now.Year, i, 1)).ToString("MMM"), views[i - 1].Item3]);
            }

            var kissesStats = new List<object[]>();

            kissesStats.Add(["Month", "Kisses"]);

            for (int i = 1; i < 13; i++)
            {
                kissesStats.Add([(new DateTime(DateTime.Now.Year, i, 1)).ToString("MMM"), kisses[i - 1].Item3]);
            }

            var photoUploadStats = new List<object[]>();

            photoUploadStats.Add(["Month", "Photos"]);

            for (int i = 1; i < 13; i++)
            {
                photoUploadStats.Add([(new DateTime(DateTime.Now.Year, i, 1)).ToString("MMM"), photo[i - 1].Item3]);
            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Profile = new
                {
                    scoreStats = new[] { new[] { "Profile", "Completed" }, new object[] { "Progress", profileScore.Item2 }, new object[] { "Pending", profileScore.Item1 } },
                    likes = new
                    {
                        stats = likesStats,
                        count = likes[0].Item4,
                        total = likes[0].Item5,
                    },
                    dislikes = new
                    {
                        stats = dislikesStats,
                        count = dislikes[0].Item4,
                        total = dislikes[0].Item5,
                    },
                    views = new
                    {
                        stats = profileViewStats,
                        count = views[0].Item4,
                        total = views[0].Item5
                    }
                },
                Kisses = new
                {
                    stats = kissesStats,
                    count = kisses[0].Item4,
                    total = kisses[0].Item5,
                },
                Photo = new
                {
                    stats = photoUploadStats,
                    count = photo[0].Item4,
                    total = photo[0].Item5
                },
                Friends = new
                {
                    count = friends.Item1,
                    total = friends.Item2
                },
                Favorites = new
                {
                    count = favorites.Item1,
                    total = favorites.Item2
                },
                BlockedList = new
                {
                    count = blockedList.Item1,
                    total = blockedList.Item2
                }
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("profile/picture")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateProfilePicture([FromBody][BindRequired] PhotoRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.UpdateProfilePicture Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profile = await _profilesService.GetByUserIdAsync(userId);

            #region Profile Picture updated

            bool is_photo_uploaded = false;

            var user = await _usersService.GetAsync(userId);

            if (user!.is_photo_uploaded)
            {
                is_photo_uploaded = true;
            }
            else if (!user!.is_photo_uploaded)
            {
                if (profile.photo != model.photo)
                {
                    is_photo_uploaded = true;
                }
            }

            user.is_photo_uploaded = is_photo_uploaded;
            await _usersService.UpdateAsync(userId, user);

            #endregion

            profile.photo = !string.IsNullOrEmpty(model.photo) ? model.photo : null;
            profile.updated_at = DateTime.Now.ToUniversalTime();

            await _profilesService.UpdateAsync(profile.Id!, profile);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your profile. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my profile </a></p>", NotificationType.me, userId);

            #endregion 


            var updatedUser = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedUser;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("profile/personal-details")]
        public async Task<ActionResult<ResponseModel<object>>> UpdatePersonalDetails([FromBody][BindRequired] PersonalDetailsRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.UpdatePersonalDetails Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profile = await _profilesService.GetByUserIdAsync(userId);

            if ((profile.date_of_birth != model.date_of_birth))
            {
                // Add message to queue
                await _rabitMQProducerService.SendAsync(JsonConvert.SerializeObject(new { userId = userId }), "HAPPY_SUGAR_DADDY_EXCHANGE", "HAPPY_SUGAR_DADDY_APP");
            }

            profile.first_name = model.first_name;
            profile.last_name = model.last_name;
            profile.gender = model.gender;
            profile.height = model.height;
            profile.height_in_inches = SparkService.Helpers.CalculationsHelpers.ToInches(model.height);
            profile.martialStatus = model.martialStatus;
            profile.phone_number = model.phone_number;
            profile.date_of_birth = model.date_of_birth;
            profile.bodyType = model.bodyType;
            profile.race = model.race;
            profile.updated_at = DateTime.Now.ToUniversalTime();

            await _profilesService.UpdateAsync(profile.Id!, profile);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your profile. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my profile </a></p>", NotificationType.me, userId);

            #endregion 

            var updatedUser = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedUser;

            return Ok(responseModel);

        }


        [MapToApiVersion(1)]
        [HttpPut("profile/education")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateEducation([FromBody][BindRequired] EducationRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.UpdateEducation Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profile = await _profilesService.GetByUserIdAsync(userId);

            if ((profile.educationLevel != model.educationLevel))
            {
                // Add message to queue
                await _rabitMQProducerService.SendAsync(JsonConvert.SerializeObject(new { userId = userId }), "HAPPY_SUGAR_DADDY_EXCHANGE", "HAPPY_SUGAR_DADDY_APP");
            }


            profile.educationLevel = model.educationLevel;
            profile.updated_at = DateTime.Now.ToUniversalTime();

            await _profilesService.UpdateAsync(profile.Id!, profile);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your profile. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my profile </a></p>", NotificationType.me, userId);

            #endregion 


            var updatedUser = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedUser;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("profile/address")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateAddress([FromBody][BindRequired] AddressRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.UpdateAddress Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profile = await _profilesService.GetByUserIdAsync(userId);

            profile.address = model.address;
            profile.address2 = model.address2;
            profile.city = model.city;
            profile.country = model.country;
            profile.state = model.state;
            profile.zip_code = model.zip_code;
            profile.updated_at = DateTime.Now.ToUniversalTime();

            await _profilesService.UpdateAsync(profile.Id!, profile);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your profile. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my profile </a></p>", NotificationType.me, userId);

            #endregion 

            var updatedUser = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedUser;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("profile/about-me")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateAboutMe([FromBody][BindRequired] AboutMeRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.UpdateAboutMe Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profile = await _profilesService.GetByUserIdAsync(userId);

            profile.profileHeadline = model.profileHeadline;
            profile.describeThePersonYouAreLookingFor = model.describeThePersonYouAreLookingFor;
            profile.aboutYourselfInYourOwnWords = model.aboutYourselfInYourOwnWords;
            profile.bio = model.bio;
            profile.updated_at = DateTime.Now.ToUniversalTime();

            await _profilesService.UpdateAsync(profile.Id!, profile);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your profile. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my profile </a></p>", NotificationType.me, userId);

            #endregion 

            var updatedUser = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedUser;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("profile/interested-in")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateInterestedIn([FromBody][BindRequired] InterestedInRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.MeController.UpdateInterestedIn Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var profile = await _profilesService.GetByUserIdAsync(userId);


            if ((profile.relationshipGoals != model.relationshipGoals))
            {
                // Add message to queue
                await _rabitMQProducerService.SendAsync(JsonConvert.SerializeObject(new { userId = userId }), "HAPPY_SUGAR_DADDY_EXCHANGE", "HAPPY_SUGAR_DADDY_APP");
            }

            profile.relationshipGoals = model.relationshipGoals;
            profile.seeking = model.seeking;
            profile.iam = model.iam;
            profile.updated_at = DateTime.Now.ToUniversalTime();

            await _profilesService.UpdateAsync(profile.Id!, profile);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your profile. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my profile </a></p>", NotificationType.me, userId);

            #endregion     

            var updatedUser = _usersService.GetDetailed(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedUser;

            return Ok(responseModel);
        }

    }
}
