using SparkApp.APIModel.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Security.Claims;
using SparkApp.APIModel.User;
using RabbitMQ.Client.Core.DependencyInjection.Services.Interfaces;
using RabbitMQ.Client.Core.DependencyInjection.Services;
using Newtonsoft.Json;
using SparkService.Services;
using SparkService.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SparkApp.Helper;
using System.IdentityModel.Tokens.Jwt;
using SparkService.ViewModels;
using SparkApp.APIModel.Member;
using Asp.Versioning;
using SparkApp.AuthorizationHandler;
using SparkApp.Security.Policy;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SparkApp.Services;
using Microsoft.Extensions.Options;
using SparkApp.APIModel.Notifications;
using System.Linq.Expressions;
using PayPal.v1.BillingPlans;
using SparkService.Helpers;
using System.Drawing;
using MongoDB.Bson;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using SparkApp.Models;
using System.Drawing.Printing;


namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/users")]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly AppSettings _config;
        private readonly IAuthorizationService _authorizationService;
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
        private readonly LikesDisLikesProfilesService _likesDisLikesProfilesService;
        private readonly FavoritesService _favoritesService;
        private readonly KissesService _kissesService;
        private readonly NotificationsService _notificationsService;
        private readonly UserNotificationService _userNotificationService;
        private readonly SubscriptionService _subscriptionService;
        private readonly FileService _fileService;



        public UserController(ILogger<UserController> logger, IAuthorizationService authorizationService, UsersService usersService, ProfilesService profilesService, UserRolesService userRolesService, RolesService rolesService, EncryptionService encryptionService, TraitsService traitsService, InterestsService interestsService, IProducingService rabitMQProducerService, PhotosService photosService, MailService mailService, EmailVerificationRequestsService emailVerificationRequestsService, LikesDisLikesProfilesService likesDisLikesProfilesService, FavoritesService favoritesService, KissesService kissesService, NotificationsService notificationsService, UserNotificationService userNotificationService, SubscriptionService subscriptionService, FileService fileService, IOptions<AppSettings> config)
           => (_logger, _authorizationService, _usersService, _profilesService, _userRolesService, _rolesService, _encryptionService, _traitsService, _interestsService, _rabitMQProducerService, _photosService, _mailService, _emailVerificationRequestsService, _likesDisLikesProfilesService, _favoritesService, _kissesService, _notificationsService, _userNotificationService, _subscriptionService, _fileService, _config) = (logger, authorizationService, usersService, profilesService, userRolesService, rolesService, encryptionService, traitsService, interestsService, rabitMQProducerService, photosService, mailService, emailVerificationRequestsService, likesDisLikesProfilesService, favoritesService, kissesService, notificationsService, userNotificationService, subscriptionService, fileService, config.Value);


        [MapToApiVersion(1)]
        [HttpGet("search/v1/{page:int}/{pageSize:int}/{term?}")]
        public ActionResult<ResponseModel<object>> GetUsersPaged([FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize, [FromRoute] string? term)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var result = _usersService.GetAllUsersPaged(page, pageSize, term);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Item2,
                Items = result.Item1,
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost]
        public async Task<ActionResult<ResponseModel<UserViewModel>>> Create([FromBody] UserCreateRequestModel registerUser)
        {
            ResponseModel<UserViewModel> responseModel = new ResponseModel<UserViewModel>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Register Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception("Validation failed.Fields not valid.");
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new User(), Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            // checking if username already exists.           
            if ((await _usersService.GetByUsernameAsync(registerUser.username)) is not null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Register Error = Username already exists, username={registerUser.username}");
                throw new Exception("Username already exists, please choose another username.");
            }

            // checking if email already exists.
            if ((await _usersService.GetByEmailAsync(registerUser.email_address)) is not null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Register Error = Email already exists, email={registerUser.email_address}");
                throw new Exception("Email already exists, please choose another email.");
            }

            var role = await _rolesService.GetByNameAsync("User");

            User newUser = new User();
            newUser.email_address = registerUser.email_address;
            newUser.username = registerUser.username;
            newUser.created_at = DateTime.UtcNow;
            newUser.password = _encryptionService.Encrypt(registerUser.password);
            newUser.timezone = registerUser.timezone;
            newUser.language = registerUser.language;
            newUser.ip_address = IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString();
            newUser.is_active = true;
            newUser.is_email_verified = false;


            // Adding new user to users
            await _usersService.CreateAsync(newUser);

            #region Adding user profile Image           

            Guid newProfilePictureFileName = Guid.NewGuid();
            string newProfilePictureExtention = "jpg";
            var pathToSaveProfilePicture = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", newUser.Id!, "profile");

            if (!Directory.Exists(pathToSaveProfilePicture))
            {
                Directory.CreateDirectory(pathToSaveProfilePicture);
            }

            SparkService.Models.File generatedProfilePictureFile = new SparkService.Models.File();

            generatedProfilePictureFile.originalName = string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention);
            generatedProfilePictureFile.name = string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention);

            using (var image = ImageHandler.GenerateRactangle(registerUser.firstName, registerUser.lastName))
            {
                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "original")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "original"));
                }

                var fullPathOriginal = Path.Combine(pathToSaveProfilePicture, "original", string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention));

                using (FileStream file = new FileStream(fullPathOriginal, FileMode.Create, FileAccess.Write))
                {
                    image.WriteTo(file);
                }

                generatedProfilePictureFile.path_original = fullPathOriginal;
                generatedProfilePictureFile.query_original = string.Format("/{0}/{1}/{2}/{3}", newUser.Id!, "profile", "original", string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention));


                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "400x320")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "400x320"));
                }

                var fullPath400x320 = Path.Combine(pathToSaveProfilePicture, "400x320", string.Format("{0}_400x320.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 480, 320, 100L, fullPath400x320);
                generatedProfilePictureFile.path_480x320 = fullPath400x320;
                generatedProfilePictureFile.query_480x320 = string.Format("/{0}/{1}/{2}/{3}", newUser.Id!, "profile", "400x320", string.Format("{0}_400x320.{1}", newProfilePictureFileName, newProfilePictureExtention));


                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "300x300")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "300x300"));
                }
                var fullPath300x300 = Path.Combine(pathToSaveProfilePicture, "300x300", string.Format("{0}_300x300.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 300, 300, 100L, fullPath300x300);
                generatedProfilePictureFile.path_300x300 = fullPath300x300;
                generatedProfilePictureFile.query_300x300 = string.Format("/{0}/{1}/{2}/{3}", newUser.Id!, "profile", "300x300", string.Format("{0}_300x300.{1}", newProfilePictureFileName, newProfilePictureExtention));



                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "100x100")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "100x100"));
                }
                var fullPath100x100 = Path.Combine(pathToSaveProfilePicture, "100x100", string.Format("{0}_100x100.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 100, 100, 100L, fullPath100x100);
                generatedProfilePictureFile.path_100x100 = fullPath100x100;
                generatedProfilePictureFile.query_100x100 = string.Format("/{0}/{1}/{2}/{3}", newUser.Id!, "profile", "100x100", string.Format("{0}_100x100.{1}", newProfilePictureFileName, newProfilePictureExtention));



                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "32x32")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "32x32"));
                }
                var fullPath32x32 = Path.Combine(pathToSaveProfilePicture, "32x32", string.Format("{0}_32x32.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 32, 32, 100L, fullPath32x32);
                generatedProfilePictureFile.path_32x32 = fullPath32x32;
                generatedProfilePictureFile.query_32x32 = string.Format("/{0}/{1}/{2}/{3}", newUser.Id!, "profile", "32x32", string.Format("{0}_32x32.{1}", newProfilePictureFileName, newProfilePictureExtention));



                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "16x16")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "16x16"));
                }
                var fullPath16x16 = Path.Combine(pathToSaveProfilePicture, "16x16", string.Format("{0}_16x16.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 16, 16, 100L, fullPath16x16);
                generatedProfilePictureFile.path_16x16 = fullPath16x16;
                generatedProfilePictureFile.query_16x16 = string.Format("/{0}/{1}/{2}/{3}", newUser.Id!, "profile", "16x16", string.Format("{0}_16x16.{1}", newProfilePictureFileName, newProfilePictureExtention));

                generatedProfilePictureFile.size = ((image.ToArray().Length / 1024f) / 1024f);
            }

            generatedProfilePictureFile.type = "image/jpeg";
            generatedProfilePictureFile.created_at = DateTime.Now.ToUniversalTime();

            await _fileService.CreateAsync(generatedProfilePictureFile);

            #endregion

            Profile newUserProfile = new Profile();
            newUserProfile.UserId = newUser.Id;
            newUserProfile.first_name = registerUser.firstName;
            newUserProfile.last_name = registerUser.lastName;
            newUserProfile.date_of_birth = registerUser.date_of_birth;
            newUserProfile.gender = registerUser.gender;
            newUserProfile.iam = registerUser.iam;
            newUserProfile.race = registerUser.race;
            newUserProfile.martialStatus = registerUser.martialStatus;
            newUserProfile.bodyType = registerUser.bodyType;
            newUserProfile.seeking = registerUser.seeking;
            newUserProfile.height = registerUser.height;
            newUserProfile.annualIncome = registerUser.annualIncome;
            newUserProfile.country = registerUser.country;
            newUserProfile.state = registerUser.state;
            newUserProfile.city = registerUser.city;
            newUserProfile.zip_code = registerUser.zip;
            newUserProfile.profileHeadline = registerUser.profileHeadline;
            newUserProfile.aboutYourselfInYourOwnWords = registerUser.aboutYourselfInYourOwnWords;
            newUserProfile.describeThePersonYouAreLookingFor = registerUser.describeThePersonYouAreLookingFor;
            newUserProfile.photo = registerUser.photo;
            newUserProfile.created_at = DateTime.UtcNow;
            newUserProfile.photo = generatedProfilePictureFile.Id;

            // Adding new user profile
            await _profilesService.CreateAsync(newUserProfile);


            //Subscribing to free plan by default 

            SubscriptionPlans? subscriptionPlans = _subscriptionService.GetSubscriptionPlans().Where(x => x.type == "free").FirstOrDefault();

            if (subscriptionPlans is not null)
            {
                Subscriptions subscriptions = new Subscriptions();
                subscriptions.start_date = DateTime.UtcNow;
                subscriptions.status = SubscriptionsStatus.active.ToString();
                subscriptions.created_at = DateTime.UtcNow;
                subscriptions.subscriptionPlansId = subscriptionPlans.Id!;
                subscriptions.userId = newUser.Id!;

                await _subscriptionService.AddSubscriptionsAsync(subscriptions);
            }

            UserRoles userRoles = new UserRoles();
            userRoles.UserId = newUser.Id;
            userRoles.RoleId = role.Id;

            // Adding new user role
            await _userRolesService.CreateAsync(userRoles);

            #region Email verification 

            EmailVerificationRequests emailVerificationRequests = new EmailVerificationRequests();
            emailVerificationRequests.created_at = DateTime.Now.ToUniversalTime();
            emailVerificationRequests.UserId = newUser.Id;
            emailVerificationRequests.token = Guid.NewGuid().ToString();
            emailVerificationRequests.expiration_at = DateTime.Now.ToUniversalTime().AddDays(1);

            // Adding new user email verification
            await _emailVerificationRequestsService.CreateAsync(emailVerificationRequests);

            // Sending verification email
            bool emailStatus = await _mailService.SendEmailVerification(newUser, newUserProfile, emailVerificationRequests);

            #endregion


            responseModel.Success = true;
            responseModel.Message = "Registration Successful";
            responseModel.Data = _usersService.GetDetailed(newUser.Id);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<UserViewModelV2>>> Get([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _usersService.GetAsync(id), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var member = _usersService.GetDetailedV2(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = member;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("{id}/photos/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> EditPhoto([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string photoId, [FromBody] EditUserPhotoRequestModel model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.EditPhoto Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            var photo = await _photosService.GetAsync(photoId);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, photo, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            // Clearing previous set featured photos.
            if (model.is_featured)
            {
                await _photosService.RemoveFeaturedAttributeFromPhotos(id);
            }


            photo.is_featured = model.is_featured;
            photo.is_private = model.is_private;
            photo.is_adult = model.is_adult;
            photo.passCode = model.passCode;
            photo.updated_at = DateTime.UtcNow;
            photo.is_members_only = model.is_members_only;

            await _photosService.UpdateAsync(photoId, photo);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your photos. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Photos </a></p>", NotificationType.photos, id);

            #endregion

            var userPhotos = _photosService.GetDetailed(photoId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = userPhotos;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("{id}/traits")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateTraits([FromRoute][BindRequired] string id, [FromBody] UserTraitsRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.ChangePassword Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            foreach (var item in model.traits)
            {

                var userTrait = _traitsService.GetUserTraitsById(id, item.trait_id);

                if (userTrait is not null)
                {
                    #region Authorize Handler

                    var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, userTrait, Operations.Update);

                    if (!isAuthorized.Succeeded)
                    {
                        return Forbid();
                    }

                    #endregion

                    userTrait.trait_value = item.trait_value;
                    await _traitsService.UpdateUserTraitsAsync(userTrait.Id!, userTrait);
                }
                else
                {

                    UserTraits userTraits = new UserTraits();
                    userTraits.user_id = id;
                    userTraits.trait_id = item.trait_id;
                    userTraits.trait_value = item.trait_value;

                    #region Authorize Handler

                    var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, userTraits, Operations.Create);

                    if (!isAuthorized.Succeeded)
                    {
                        return Forbid();
                    }

                    #endregion

                    await _traitsService.AddUserTraits(userTraits);
                }
            }


            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your traits. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my traits </a></p>", NotificationType.traits, id);

            #endregion

            var traits = _traitsService.GetUserTraits(id);

            // Add message to queue
            await _rabitMQProducerService.SendAsync(JsonConvert.SerializeObject(new { userId = id }), "HAPPY_SUGAR_DADDY_EXCHANGE", "HAPPY_SUGAR_DADDY_APP");

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = traits;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("{id}/interests")]
        public async Task<ActionResult<ResponseModel<object>>> UpdateInterests([FromRoute][BindRequired] string id, [FromBody] UserInterestsRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.ChangePassword Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            var prevInterests = _interestsService.GetUserInterests(id);

            foreach (var item in prevInterests)
            {
                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _interestsService.GetAsync(item.Id!), Operations.Delete);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _interestsService.RemoveInterestsAsync(item.Id!);
            }

            foreach (var item in model.interests)
            {

                var interest = new Interests
                {
                    category_id = item.category_id,
                    created_at = DateTime.UtcNow,
                    created_by = id,
                    interest_description = item.interest_description,
                    is_active = true,
                    is_featured = false,
                    modified_at = DateTime.UtcNow,
                    modified_by = id,
                    popularity = item.popularity
                };

                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, interest, Operations.Create);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _interestsService.CreateInterestsAsync(interest);
            }

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have updated your interests. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Check my interests </a></p>", NotificationType.interests, id);

            #endregion

            var interests = _interestsService.GetUserInterests(id);

            // Add message to queue
            await _rabitMQProducerService.SendAsync(JsonConvert.SerializeObject(new { userId = id }), "HAPPY_SUGAR_DADDY_EXCHANGE", "HAPPY_SUGAR_DADDY_APP");


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = interests;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("{id}/photos")]
        public async Task<ActionResult<ResponseModel<object>>> UpdatePhotos([FromRoute][BindRequired] string id, [FromBody] UserPhotosRequestModel model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.UpdatePhotos Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            foreach (var photo in model.Photos)
            {
                var newPhoto = new Photos
                {
                    created_at = DateTime.UtcNow,
                    fileId = photo.fileId,
                    is_adult = photo.is_adult,
                    is_featured = photo.is_featured,
                    is_private = photo.is_private,
                    passCode = photo.passCode,
                    updated_at = DateTime.UtcNow,
                    is_members_only = photo.is_members_only,
                    userId = id
                };

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, newPhoto, Operations.Create);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                await _photosService.CreateAsync(newPhoto);
            }

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have added photos. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Photos </a></p>", NotificationType.photos, id);

            #endregion

            var userPhotos = _photosService.GetUserPhotos(id, string.Empty, 1, 10);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = userPhotos.Item2,
                Items = userPhotos.Item1,
                page = 1,
                pageSize = 10
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("{id}/search/v2/{page:int}/{pageSize:int}")]
        public ActionResult<ResponseModel<object>> Search(SearchRequestModel criteria)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var users = _usersService.Filter(criteria.id,
                criteria.SearchRequest.iam ?? "",
                criteria.SearchRequest.seeking ?? "",
                criteria.SearchRequest.ageFrom ?? "",
                criteria.SearchRequest.ageTo ?? "",
                criteria.SearchRequest.race!,
                criteria.SearchRequest.gender!,
                criteria.SearchRequest.educationLevel!,
                criteria.SearchRequest.heightFrom ?? "",
                criteria.SearchRequest.heightTo ?? "",
                criteria.SearchRequest.martialStatus ?? "",
                criteria.SearchRequest.income!,
                criteria.SearchRequest.bodyType!,
                criteria.SearchRequest.country!,
                criteria.page,
                criteria.pageSize);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = users.Item2,
                Items = users.Item1,
                page = criteria.page,
                pageSize = criteria.pageSize
            };

            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet("recent-joined/{iam}/{page:int}/{pageSize:int}")]
        public ActionResult<ResponseModel<object>> RecentMembers([FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize, [FromRoute][BindRequired] string iam)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (string.IsNullOrEmpty(iam))
            {
                throw new Exception("I am field cannot be left blank");
            }

            var members = _usersService.RecentMembers100(iam);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = members.Count(),
                Items = members.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };
            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/traits")]
        public async Task<ActionResult<ResponseModel<object>>> Traits([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new UserTraits() { user_id = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            var traits = _traitsService.GetUserTraits(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = traits;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/interests")]
        public async Task<ActionResult<ResponseModel<object>>> Interests([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();


            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Interests() { created_by = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            var interests = _interestsService.GetUserInterests(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = interests;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("{id}/photos/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> Photos(UserPhotoSearchRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Photos() { userId = model.id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion


            var userPhotos = _photosService.GetUserPhotos(model.id, model.PhotoSearch.term, model.page, model.pageSize);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = userPhotos.Item2,
                Items = userPhotos.Item1,
                page = model.page,
                pageSize = model.pageSize
            };
            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/kisses-received/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetKissesReceived([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Kisses() { kissed_id = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion


            var result = _kissesService.GetKissesReceived(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Count(),
                Items = result.Skip(((page - 1) * pageSize)).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);

        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/kisses/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetKisses([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Kisses() { user_id = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion


            var result = _kissesService.GetKisses(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Count(),
                Items = result.Skip(((page - 1) * pageSize)).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);

        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/favorites/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetFavorites([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Favorites() { user_id = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            var result = _favoritesService.GetFavoritess(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Count(),
                Items = result.Skip(((page - 1) * pageSize)).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/dislikes/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetDisLikesForUser([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new LikesDisLikesProfiles() { user_id = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion


            var result = _likesDisLikesProfilesService.GetDisLikesForUser(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Count(),
                Items = result.Skip(((page - 1) * pageSize)).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/likes/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetLikesForUser([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new LikesDisLikesProfiles() { user_id = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            var result = _likesDisLikesProfilesService.GetLikesForUser(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Count(),
                Items = result.Skip(((page - 1) * pageSize)).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);

        }


        [MapToApiVersion(1)]
        [HttpPost("likes-dislikes")]
        public async Task<ActionResult<ResponseModel<object>>> AddLikesDisLikes([FromBody] LikesDisLikesProfilesRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (model is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.AddLikesDisLikes Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            var likesDisLikesProfiles = new LikesDisLikesProfiles();
            likesDisLikesProfiles.isLikes = model.isLikes;
            likesDisLikesProfiles.profile_id = model.profile_id;
            likesDisLikesProfiles.user_id = model.user_id;
            likesDisLikesProfiles.created_at = DateTime.UtcNow;
            likesDisLikesProfiles.updated_at = DateTime.UtcNow;


            var checkIfLikeOrDislikeAlreadyExists = await _likesDisLikesProfilesService.GetAsync(model.user_id, model.profile_id);

            if (checkIfLikeOrDislikeAlreadyExists is not null)
            {
                if (checkIfLikeOrDislikeAlreadyExists.isLikes == model.isLikes)
                {
                    throw new Exception($"Already exists " + (model.isLikes ? "like" : "disLike") + " for this profile.");
                }

                checkIfLikeOrDislikeAlreadyExists.isLikes = model.isLikes;
                checkIfLikeOrDislikeAlreadyExists.updated_at = DateTime.UtcNow;

                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, checkIfLikeOrDislikeAlreadyExists, Operations.Update);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _likesDisLikesProfilesService.UpdateAsync(checkIfLikeOrDislikeAlreadyExists.Id!, checkIfLikeOrDislikeAlreadyExists);

                #region Notification 

                await _userNotificationService.SendNotificationForEvent($"<p>You have received another {(model.isLikes ? "like" : "disLike")}. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Photos </a></p>", NotificationType.like_or_dislike, model.profile_id);

                #endregion
            }
            else
            {
                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, likesDisLikesProfiles, Operations.Create);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _likesDisLikesProfilesService.AddLikesDisLikes(likesDisLikesProfiles);

                #region Notification 

                await _userNotificationService.SendNotificationForEvent($"<p>You have received {(model.isLikes ? "like" : "disLike")}. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Photos </a></p>", NotificationType.like_or_dislike, model.profile_id);

                #endregion
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = model;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("favorites")]
        public async Task<ActionResult<ResponseModel<object>>> AddFavorites([FromBody] FavoriteRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (model is null)
            {
                _logger.LogError($"SparkApp.Controllers.MemberController.AddFavorites Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }


            var favorites = new Favorites();
            favorites.favorite_id = model.favorite_id;
            favorites.user_id = model.user_id;
            favorites.type = (FavoritesTypes.user).ToString();
            favorites.created_at = DateTime.UtcNow;
            favorites.updated_at = DateTime.UtcNow;

            var checkIfFavoritesAlreadyExists = await _favoritesService.GetAsync(model.user_id, model.favorite_id);

            if (checkIfFavoritesAlreadyExists is not null)
            {
                throw new Exception("Already added to favorite.");
            }
            else
            {

                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, favorites, Operations.Create);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _favoritesService.AddFavoritess(favorites);

                #region Notification 

                await _userNotificationService.SendNotificationForEvent($"<p>You have added another member to your favorites. <a href='{string.Format("{0}/dashboard", _config.ClientUrl)}' > Favorites </a></p>", NotificationType.favorite, this.User.Claims.FirstOrDefault(x => x.Type.Equals("id", StringComparison.OrdinalIgnoreCase))!.Value);

                #endregion
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = model;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("kisses")]
        public async Task<ActionResult<ResponseModel<object>>> AddKisses([FromBody] KissesRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (model is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.AddKisses Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            var checkIfKissesAlreadyExists = await _kissesService.GetAsync(model.user_id, model.kissed_id);

            if (checkIfKissesAlreadyExists is not null)
            {
                checkIfKissesAlreadyExists.kissed_count = checkIfKissesAlreadyExists.kissed_count + 1;

                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, checkIfKissesAlreadyExists, Operations.Update);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _kissesService.UpdateAsync(checkIfKissesAlreadyExists.Id!, checkIfKissesAlreadyExists);

                #region Notification 

                await _userNotificationService.SendNotificationForEvent($"<p>You have received another Kiss. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Kisses </a></p>", NotificationType.kisses, model.kissed_id);

                #endregion
            }
            else
            {
                var kisses = new Kisses();
                kisses.kissed_id = model.kissed_id;
                kisses.user_id = model.user_id;
                kisses.created_at = DateTime.UtcNow;
                kisses.updated_at = DateTime.UtcNow;
                kisses.kissed_count = 1;

                #region Authorize Handler

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, kisses, Operations.Create);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                #endregion

                await _kissesService.AddKisses(kisses);

                #region Notification 

                await _userNotificationService.SendNotificationForEvent($"<p>You have received Kiss. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Kisses </a></p>", NotificationType.kisses, model.kissed_id);

                #endregion
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = model;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/member-photos/{page}/{pageSize}")]
        public async Task<ActionResult<ResponseModel<object>>> MemberPhotos([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError($"SparkApp.Controllers.UserController.Photos Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Photos() { userId = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion


            var userPhotos = _photosService.GetMemberPhotos(id, page, pageSize);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = userPhotos.Item2,
                Items = userPhotos.Item1,
                Featured = _photosService.GetUserFeaturedPhoto(id),
                page = page,
                pageSize = pageSize
            };
            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("username/{username}")]
        public async Task<ActionResult<ResponseModel<object>>> GetMemberByUsername([FromRoute][BindRequired] string username)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _usersService.GetByUsernameAsync(username), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<User> members = new List<User>();

            if (!string.IsNullOrEmpty(username))
            {
                members = _usersService.SearchByUsernameAsync(username);
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = members;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("{id}/notifications/read")]
        public async Task<ActionResult<ResponseModel<object>>> MarkNotificationsRead([FromRoute][BindRequired] string id, [FromBody] NotificationMarkReadRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (model is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.MarkNotificationsRead Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }

            foreach (var item in model.notifications)
            {

                var notification = await _notificationsService.GetAsync(item);

                if (notification is not null)
                {

                    notification.is_read = true;

                    #region Authorize Handler

                    var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, notification, Operations.Update);

                    if (!isAuthorized.Succeeded)
                    {
                        return Forbid();
                    }

                    #endregion

                    await _notificationsService.UpdateAsync(item, notification);
                }
            }

            var notifications = _notificationsService.Get(id);

            int total = notifications.Count();
            var items = notifications.OrderByDescending(x => x.created_at).Skip(((1 - 1) * 10)).Take(10).ToList();

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = total,
                Items = items,
                page = 1,
                pageSize = 10
            };

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [AllowAnonymous]
        [HttpGet("{id}/subscription/payments/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetSubscriptionPayments([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new SubscriptionPayments() { userId = id }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            var result = (from p in _subscriptionService.GetSubscriptionPayments()
                          join s in _subscriptionService.GetSubscriptions() on p.subscriptionId equals s.Id
                          join t in _subscriptionService.GetSubscriptionPlans() on s.subscriptionPlansId equals t.Id
                          where p.userId == id
                          select new { Payment = p, Plan = t, Subscription = s }
                          );


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = result.Count(),
                Items = result.Skip(((page - 1) * pageSize)).Take(pageSize).Select(p =>
                new
                {
                    amount = p.Payment.amount,
                    created_at = p.Payment.created_at,
                    source = p.Payment.source,
                    status = p.Payment.status,
                    plan = p.Plan,
                }).ToList(),
                page = page,
                pageSize = pageSize
            };
            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpGet("search/v3/{term?}/{pageSize:int}")]
        public ActionResult<ResponseModel<object>> GetUsers([FromRoute] string? term, int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var result = _usersService.GetAllUsers(term, pageSize);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = result;
            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpGet("{id}/member/{memberId}")]
        public ActionResult<ResponseModel<UserViewModel>> GetMemberById([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string memberId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var member = _usersService.GetMemberById(memberId);

            if (member is not null)
            {
                var isLikes = Task.Run(async () => await _usersService.GetLikeForUserAsync(id, memberId)).Result;

                if (isLikes is null)
                {
                    member.isLike = null;
                }
                else
                {
                    member.isLike = isLikes.isLikes;
                }

                var isFavorite = Task.Run(async () => await _usersService.GetFavoritesAsync(id, memberId)).Result;

                if (isFavorite is not null)
                {
                    member.isFavorite = true;
                }

                var isKissed = Task.Run(async () => await _usersService.GetKissesAsync(id, memberId)).Result;

                if (isKissed is not null)
                {
                    member.iskissed = true;
                }

                var isBlocked = Task.Run(async () => await _usersService.GetBlockedListForMember(id, memberId)).Result;

                if (isBlocked is not null)
                {
                    member.isBlocked = true;
                }
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = member;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpDelete("{id}/photo-delete/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> RemovePhoto([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string photoId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var photo = await _photosService.GetAsync(photoId);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.RemovePhoto Error = Photo not found");
                throw new Exception($"SparkApp.Controllers.UserController.RemovePhoto Error = Photo not found");
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, photo, Operations.Delete);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _photosService.DeletePhoto(photoId);


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = photoId;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpGet("{id}/storage")]
        public async Task<ActionResult<ResponseModel<object>>> GetStorageDetails([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId != id)
            {
                return Forbid();
            }

            var user = await _usersService.GetAsync(id);

            if (user is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetStorageDetails Error = User not found");
                throw new Exception($"SparkApp.Controllers.UserController.GetStorageDetails Error = User not found");
            }

            var subscription = _subscriptionService.GetSubscription(userId!);
            if (subscription is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetStorageDetails Error = no subscription not found");
                throw new Exception($"SparkApp.Controllers.UserController.GetStorageDetails Error = no subscription not found");
            }


            var pathToUserDirectory = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", id, "gallery");
            if (!Directory.Exists(pathToUserDirectory))
            {
                responseModel.Success = true;
                responseModel.Message = "Success";
                responseModel.Data = new
                {
                    TotalSize = subscription.Plan.storage,
                    UsedSize = 0,
                    FreeSize = subscription.Plan.storage,
                    Unit = "MB"
                };
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                TotalSize = subscription.Plan.storage,
                UsedSize = _photosService.GetUserGallerySize(id),
                FreeSize = subscription.Plan.storage,
                Unit = "MB"
            };

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPost("{id}/photo-unlock/{photoId}")]
        public ActionResult<ResponseModel<object>> GetPhotoByUnlockCode(UserPhotoUnlockRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();


            PhotosViewModel? photo = _photosService.GetDetailed(model.photoId!);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetPhotoByUnlockCode Error = Photo not found ");
                throw new Exception($"Photo not found");
            }

            if (!photo.is_private)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetPhotoByUnlockCode Error = Photo not private ");
                throw new Exception($"Photo is not private");
            }

            if (photo.passCode != model.UnlockCredentials.code)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetPhotoByUnlockCode Error = Invalid unlock code");
                throw new Exception($"Invalid unlock code");
            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = photo;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/photo-likes-dislikes/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> GetLikesDislikesPhoto([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string photoId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var photo = await _photosService.GetAsync(photoId);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetLikesDislikesPhoto Error = Photo not found ");
                throw new Exception($"Photo not found");
            }

            var likesDislikesPhoto = _photosService.GetLikesDislikesPhotoSummary(id, photoId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = likesDislikesPhoto;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPost("{id}/photo-likes-dislikes/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> AddLikesOrDisLikesToPhoto(LikesOrDisLikesCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var photo = await _photosService.GetAsync(model.photoId);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.AddLikesOrDisLikesToPhoto Error = Photo not found");
                throw new Exception($"Photo not found");
            }

            LikesDisLikesPhoto? likesDisLikesPhoto = _photosService.GetLikesDisLikesPhoto(model.id, model.photoId);
            if (likesDisLikesPhoto is null)
            {
                LikesDisLikesPhoto newLikesDisLikesPhoto = new LikesDisLikesPhoto();
                newLikesDisLikesPhoto.photo_id = model.photoId;
                newLikesDisLikesPhoto.isLikes = model.LikesOrDisLikesPhoto.IsLike;
                newLikesDisLikesPhoto.disLikes = model.LikesOrDisLikesPhoto.IsDislike;
                newLikesDisLikesPhoto.user_id = model.id;
                newLikesDisLikesPhoto.created_at = DateTime.Now.ToUniversalTime();

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, newLikesDisLikesPhoto, Operations.Create);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                await _photosService.CreateLikesDislikesPhotoAsync(newLikesDisLikesPhoto);
            }
            else
            {
                likesDisLikesPhoto.disLikes = model.LikesOrDisLikesPhoto.IsDislike;
                likesDisLikesPhoto.isLikes = model.LikesOrDisLikesPhoto.IsLike;
                likesDisLikesPhoto.updated_at = DateTime.Now.ToUniversalTime();

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, likesDisLikesPhoto, Operations.Update);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                await _photosService.UpdateLikesDisLikesPhotoAsync(likesDisLikesPhoto.Id!, likesDisLikesPhoto);

            }


            var likesDislikesPhoto = _photosService.GetLikesDislikesPhotoSummary(model.id, model.photoId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = likesDislikesPhoto;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}/photo-views/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> GetViewsForPhoto([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string photoId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var photo = await _photosService.GetAsync(photoId);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetViewsForPhoto Error = Photo not found");
                throw new Exception($"Photo not found");
            }

            ViewsPhotoViewModel views = _photosService.GetViewsForPhoto(photoId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = views;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPost("{id}/photo-views/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> AddViewForPhoto(ViewsPhotoCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var photo = await _photosService.GetAsync(model.photoId);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.AddViewForPhoto Error = Photo not found");
                throw new Exception($"Photo not found");
            }

            bool doesViewExists = _photosService.DoesViewFromUserExists(model.id, model.photoId);

            if (!doesViewExists)
            {
                await _photosService.CreateViewsPhotoAsync(new ViewsPhoto
                {
                    photoId = model.photoId,
                    userId = model.id,
                    created_at = DateTime.Now.ToUniversalTime(),
                    ip_address = IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString()
                });
            }

            ViewsPhotoViewModel views = _photosService.GetViewsForPhoto(model.photoId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = views;

            return Ok(responseModel);
        }




        [MapToApiVersion(1)]
        [HttpGet("{id}/photo-stats/{photoId}")]
        public async Task<ActionResult<ResponseModel<object>>> GetPhotoStats([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string photoId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var photo = await _photosService.GetAsync(photoId);
            if (photo is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetPhotoStats Error = Photo not found");
                throw new Exception($"Photo not found");
            }


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, photo, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }


            var views = _photosService.GetPhotoViewStat(photoId);

            var like = _photosService.GetPhotoLikeStat(photoId);

            var dislike = _photosService.GetPhotoDisLikeStat(photoId);

            var result = new List<object[]>();

            result.Add(["Month", "Views", "Like", "Dislike"]);

            for (int i = 1; i < 13; i++)
            {
                result.Add([(new DateTime(DateTime.Now.Year, i, 1)).ToString("MMM"), views[i - 1].Item3, like[i - 1].Item3, dislike[i - 1].Item3]);
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = result;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpGet("{id}/member-similar/{memberId}")]
        public async Task<ActionResult<ResponseModel<object>>> GetSimilarMembers([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string memberId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var members = await _usersService.SimilarMembers(id, memberId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = members;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpGet("{id}/profile-views/{memberId}")]
        public async Task<ActionResult<ResponseModel<object>>> GetViewsForProfile([FromRoute][BindRequired] string id, [FromRoute][BindRequired] string memberId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var user = await _usersService.GetAsync(memberId);

            if (user is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.GetViewsForProfile Error = User not found");
                throw new Exception($"User not found");
            }



            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                views = await _usersService.GetViewsProfileCount(memberId)
            };

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPost("{id}/profile-views/{memberId}")]
        public async Task<ActionResult<ResponseModel<object>>> AddViewForProfile(ViewsProfileCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var user = await _usersService.GetAsync(model.memberId);

            if (user is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.AddViewForProfile Error = User not found");
                throw new Exception($"User not found");
            }

            bool doesViewExists = await _usersService.DoesViewFromUserExistsForProfile(model.id, model.memberId);

            if (!doesViewExists)
            {
                var view = new ViewsProfile();
                view.userId = model.id;
                view.profileId = model.memberId;
                view.created_at = DateTime.Now.ToUniversalTime();
                view.ip_address = IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString();
                await _usersService.CreateViewProfileAsync(view);

            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                views = await _usersService.GetViewsProfileCount(model.memberId)
            };

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPost("{id}/member-block/{memberId}")]
        public async Task<ActionResult<ResponseModel<object>>> AddToBlockList(BlockListCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var blocklistExists = await _usersService.GetBlockedListForMember(model.id, model.memberId);
            if (blocklistExists is not null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.AddToBlockList Error = User already blocked");
                throw new Exception($"User already blocked");
            }

            BlockedList blockedList = new BlockedList();
            blockedList.blocked_by = model.id;
            blockedList.member_id = model.memberId;
            blockedList.created_at = DateTime.UtcNow;


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, blockedList, Operations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _usersService.CreateBlockListAsync(blockedList);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = blockedList;

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpGet("{id}/member-block/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetBlockList([FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new BlockedList { blocked_by = id }, Operations.Read);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }


            var blocklist = _usersService.GetBlockedListMembers(page, pageSize, id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = blocklist.Item2,
                Items = blocklist.Item1,
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpDelete("{id}/member-block/{memberId}")]
        public async Task<ActionResult<ResponseModel<object>>> BlockListRemoveMember(BlockListEditRequest blockListEditRequest)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var blockedListItem = await _usersService.GetBlockedListForMember(blockListEditRequest.id, blockListEditRequest.memberId);

            if (blockedListItem is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.BlockListRemoveMember Error = Record not found");
                throw new Exception($"Record not found");
            }


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, blockedListItem, Operations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var result = await _usersService.BlockedListMemberRemove(blockListEditRequest.id, blockListEditRequest.memberId);

            if (!result.IsAcknowledged)
            {
                return BadRequest();
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                memberId = blockListEditRequest.memberId
            };

            return Ok(responseModel);
        }
    }
}
