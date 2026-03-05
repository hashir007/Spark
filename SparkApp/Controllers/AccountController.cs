using Amazon.Runtime;
using Asp.Versioning;
using SparkApp.APIModel.Administrators;
using SparkApp.APIModel.General;
using SparkApp.APIModel.User;
using SparkApp.Helper;
using SparkApp.Models;
using SparkApp.Services;
using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Extensions;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities.Net;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using WebASparkApppi.Authorization;
using static System.Net.Mime.MediaTypeNames;

namespace SparkApp.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/account")]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly AppSettings _config;
        private readonly UsersService _usersService;
        private readonly JwtUtils _jwtService;
        private readonly EncryptionService _encryptionService;
        private readonly ProfilesService _profilesService;
        private readonly UserRolesService _userRolesService;
        private readonly RolesService _rolesService;
        private readonly MailService _mailService;
        private readonly EmailVerificationRequestsService _emailVerificationRequestsService;
        private readonly UserNotificationService _userNotificationService;
        private readonly SubscriptionService _subscriptionService;
        private readonly FileService _fileService;
        private readonly ForgotPasswordRequestsService _forgotPasswordRequestsService;

        public AccountController(ILogger<AccountController> logger, UsersService usersService, JwtUtils jwtService, EncryptionService encryptionService, ProfilesService profilesService, UserRolesService userRolesService, RolesService rolesService, MailService mailService, EmailVerificationRequestsService emailVerificationRequestsService, UserNotificationService userNotificationService, SubscriptionService subscriptionService, FileService fileService, ForgotPasswordRequestsService forgotPasswordRequestsService, IOptions<AppSettings> config) =>
        (_logger, _usersService, _jwtService, _encryptionService, _profilesService, _userRolesService, _rolesService, _mailService, _emailVerificationRequestsService, _userNotificationService, _subscriptionService, _fileService, _forgotPasswordRequestsService, _config)
            = (logger, usersService, jwtService, encryptionService, profilesService, userRolesService, rolesService, mailService, emailVerificationRequestsService, userNotificationService, subscriptionService, fileService, forgotPasswordRequestsService, config.Value);


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("login")]
        public async Task<ActionResult<ResponseModel<AuthenticateResponseModel>>> Login(AuthenticateRequestModel loginUser)
        {
            ResponseModel<AuthenticateResponseModel> responseModel = new ResponseModel<AuthenticateResponseModel>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Login Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception("Validation failed.Fields not valid.");
            }

            var user = _usersService.GetAsync(loginUser.UserName, _encryptionService.Encrypt(loginUser.Password));

            if (user is null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Login Error = Invalid login, username={loginUser.UserName}");
                throw new Exception("Invalid login. Please check username or password");
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = await AuthorizeUser(user);
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("register")]
        public async Task<ActionResult<ResponseModel<AuthenticateResponseModel>>> Register(RegisterRequestModel registerUser)
        {
            ResponseModel<AuthenticateResponseModel> responseModel = new ResponseModel<AuthenticateResponseModel>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Register Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception("Validation failed.Fields not valid.");
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

            var generatedProfilePictureFile = await AddProfilePicture(newUser.Id!, registerUser.firstName, registerUser.lastName);

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
            newUserProfile.height_in_inches = SparkService.Helpers.CalculationsHelpers.ToInches(newUserProfile.height);
            newUserProfile.annualIncome = registerUser.annualIncome;
            newUserProfile.country = registerUser.country;
            newUserProfile.state = registerUser.state;
            newUserProfile.city = registerUser.city;
            newUserProfile.zip_code = registerUser.zip;
            newUserProfile.profileHeadline = registerUser.profileHeadline;
            newUserProfile.aboutYourselfInYourOwnWords = registerUser.aboutYourselfInYourOwnWords;
            newUserProfile.describeThePersonYouAreLookingFor = registerUser.describeThePersonYouAreLookingFor;
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
            responseModel.Data = await AuthorizeUser(newUser);

            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ResponseModel<AuthenticateResponseModel>>> Refresh(TokenRequestModel model)
        {
            ResponseModel<AuthenticateResponseModel> responseModel = new ResponseModel<AuthenticateResponseModel>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Refresh Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception("Validation failed.Fields not valid.");
            }

            var refreshToken = _usersService.GetRefreshToken(model.refreshToken!);
            if (refreshToken is null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Refresh Error = Refresh token not found");
                throw new Exception("Refresh token not found.");
            }

            var user = _usersService.GetUserByRefreshToken(model.refreshToken!);

            if (refreshToken.IsRevoked)
            {
                _usersService.RevokeDescendantRefreshTokens(refreshToken, IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString(), $"Attempted reuse of revoked ancestor token: {model.refreshToken}");
            }

            if (!refreshToken.IsActive)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Refresh Error = Invalid token");
                throw new Exception("Invalid token");
            }

            var newRefreshToken = _jwtService.GenerateRefreshToken(user, IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString());

            _usersService.RevokeRefreshToken(refreshToken, IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString(), newRefreshToken.token);

            await _usersService.AddRefreshToken(newRefreshToken);

            _usersService.RemoveExpiredRefreshToken(user.Id!, _config.RefreshTokenExpiryTimeDays);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.username));

            var userInRoles = await _userRolesService.GetByUserIdAsync(user.Id!);
            // Add roles as multiple claims
            foreach (var userInRole in userInRoles)
            {
                var role = await _rolesService.GetAsync(userInRole.RoleId!);

                claims.Add(new Claim(ClaimTypes.Role, role.name));
            }

            // Optionally add other app specific claims as needed
            claims.Add(new Claim("is_active", user.is_active.ToString()));

            var token = _jwtService.GenerateJwtToken(user);

            setTokenCookie(newRefreshToken.token);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new AuthenticateResponseModel()
            {
                token = token,
                expires = GetExpiryTimestamp(token),
                refreshToken = newRefreshToken.token,
                email_address = user.email_address,
                is_active = user.is_active,
                is_email_verified = user.is_email_verified,
                language = user.language,
                timezone = user.timezone,
                username = user.username,
                id = user.Id!,
                subscription = _subscriptionService.ConvertToSubscriptionV3ViewModel(_subscriptionService.GetSubscription(user.Id!)!),
            };

            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("revoke-token")]
        public ActionResult Revoke(RevokeTokenRequest model)
        {
            var token = model.refreshToken ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });


            var refreshToken = _usersService.GetRefreshToken(model.refreshToken!);
            if (refreshToken is null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.Revoke Error = Refresh token not found");
                throw new Exception("Refresh token not found.");
            }

            if (!refreshToken.IsActive)
                throw new Exception("Invalid token");

            _usersService.RevokeRefreshToken(refreshToken, IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString(), "Revoked without replacement");

            return NoContent();
        }


        [Authorize]
        [MapToApiVersion(1)]
        [HttpPost("change-password")]
        public async Task<ActionResult<ResponseModel<object>>> ChangePassword(ChangePasswordRequestModel model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.ChangePassword Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception($"Validation failed.Fields not valid. Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
            }
            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.ChangePassword Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            var user = await _usersService.GetAsync(userId);
            if (user == null)
            {
                _logger.LogError($"SparkApp.Controllers.UserController.ChangePassword Error = userId = {userId} not found.");
                throw new Exception("user not found");
            }

            if (user.password != _encryptionService.Encrypt(model.OldPassword))
            {
                throw new Exception("old password incorrect");
            }

            user.password = _encryptionService.Encrypt(model.NewPassword);
            await _usersService.UpdateAsync(user.Id!, user);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>Your password has been changed. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Accounts </a></p>", NotificationType.me, userId);

            #endregion

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = "";

            return Ok(responseModel);
        }



        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet("validate-duplicate-username/{username}")]
        public async Task<ActionResult<ResponseModel<object>>> ValidateDuplicateUsername([FromRoute][BindRequired] string username)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var result = new { exists = true };

            var user = await _usersService.ValidateUsernameExistsAsync(username);
            if (user!)
            {
                result = new { exists = false };
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = result;

            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet("validate-duplicate-email/{email}")]
        public async Task<ActionResult<ResponseModel<object>>> ValidateDuplicateEmail([FromRoute][BindRequired] string email)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var result = new { exists = true };

            var user = await _usersService.ValidateEmailExistsAsync(email);
            if (user!)
            {
                result = new { exists = false };
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = result;

            return Ok(responseModel);
        }



        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("external-login")]
        public async Task<ActionResult<ResponseModel<object>>> ExternalLogin(AuthenticationExternalRequest model)
        {
            ResponseModel<AuthenticateResponseModel> responseModel = new ResponseModel<AuthenticateResponseModel>();

            string email_address = string.Empty;
            string firstName = string.Empty;
            string lastName = string.Empty;
            string gender = string.Empty;
            string birthday = string.Empty;

            if (model.source == "FACEBOOK")
            {
                var result = await GetFacebookMe(model.access_token);
                if (result is null)
                {
                    _logger.LogError($"SparkApp.Controllers.AccountController.ExternalLogin Error = Not able to fetch user data from Facebook API");
                    throw new Exception("something went wrong.Please try again.");
                }

                email_address = result.email;
                SplitFullName(result.name, out firstName, out lastName);

            }
            else if (model.source == "GOOGLE")
            {
                var result = await GetGoogleMe(model.access_token);
                if (result is null)
                {
                    _logger.LogError($"SparkApp.Controllers.AccountController.ExternalLogin Error = Not able to fetch user data from Facebook API");
                    throw new Exception("something went wrong.Please try again.");
                }

                email_address = result.emailAddresses[0].value;
                SplitFullName(result.names[0].displayName, out firstName, out lastName);

            }


            if ((await _usersService.GetByEmailAsync(email_address)) is not null)
            {
                var user = await _usersService.GetByEmailAsync(email_address);

                responseModel.Success = true;
                responseModel.Message = "Success";
                responseModel.Data = await AuthorizeUser(user);

                return Ok(responseModel);
            }

            var role = await _rolesService.GetByNameAsync("User");

            User newUser = new User();
            newUser.email_address = email_address;
            newUser.username = $"{((new MailAddress(email_address)).User)}{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}";
            newUser.created_at = DateTime.UtcNow;
            newUser.password = _encryptionService.Encrypt(Guid.NewGuid().ToString("N"));
            newUser.timezone = model.timezone;
            newUser.language = model.language;
            newUser.ip_address = IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString();
            newUser.is_active = true;
            newUser.is_email_verified = false;

            // Adding new user to users
            await _usersService.CreateAsync(newUser);

            #region Adding user profile Image    

            var generatedProfilePictureFile = await AddProfilePicture(newUser.Id!, firstName, lastName);

            #endregion

            #region Profile

            Profile newUserProfile = new Profile();
            newUserProfile.UserId = newUser.Id;
            newUserProfile.first_name = firstName;
            newUserProfile.last_name = lastName;
            newUserProfile.created_at = DateTime.UtcNow;
            newUserProfile.photo = generatedProfilePictureFile.Id;
            if (!string.IsNullOrEmpty(gender))
            {
                newUserProfile.gender = gender!;
            }
            if (!string.IsNullOrEmpty(birthday))
            {
                newUserProfile.date_of_birth = Convert.ToDateTime(birthday);
            }

            #endregion

            // Adding new user profile
            await _profilesService.CreateAsync(newUserProfile);

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


            // Adding Email Password Reset

            #region Email verification

            // Sending verification email
            bool emailStatus = await _mailService.SendEmailNewAccountFromSocialMedia(newUser);

            #endregion 

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = await AuthorizeUser(newUser);

            return Ok(responseModel);
        }



        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ResponseModel<object>>> ForgotPasswordCreateRequest(ForgotPasswordCreateRequestModel model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if ((await _usersService.GetByEmailAsync(model.email)) is null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.ForgotPasswordCreateRequest Error = No email account found for Email = {model.email}");
                throw new Exception($"No email account found for {model.email}");
            }

            var forgotPasswordRequests = await _forgotPasswordRequestsService.GetByEmailAsync(model.email);

            foreach (var requests in forgotPasswordRequests)
            {
                requests.is_active = false;
                await _forgotPasswordRequestsService.UpdateAsync(requests.Id!, requests);
            }

            var user = await _usersService.GetByEmailAsync(model.email);

            ForgotPasswordRequests forgotPasswordCreateRequest = new ForgotPasswordRequests();
            forgotPasswordCreateRequest.email = model.email;
            forgotPasswordCreateRequest.is_active = true;
            forgotPasswordCreateRequest.token = Guid.NewGuid().ToString();
            forgotPasswordCreateRequest.completed = false;
            forgotPasswordCreateRequest.created_at = DateTime.UtcNow;
            forgotPasswordCreateRequest.userId = user.Id;
            forgotPasswordCreateRequest.expires_on = DateTime.UtcNow.AddDays(_config.ForgotPasswordExpireTimeDays);
            forgotPasswordCreateRequest.callbackUrl = model.resetPasswordCallbackUrl;

            await _forgotPasswordRequestsService.CreateAsync(forgotPasswordCreateRequest);

            await _mailService.SendEmailForgotPassWordReset(user, forgotPasswordCreateRequest);

            responseModel.Success = true;
            responseModel.Message = "Email Sent to your registered email address.";
            responseModel.Data = true;

            return Ok(responseModel);
        }



        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("reset-forgot-password/{token}")]
        public async Task<ActionResult<ResponseModel<object>>> ResetForgotPassword(ResetForgotPasswordRequest model)
        {

            ResponseModel<object> responseModel = new ResponseModel<object>();

            if ((await _forgotPasswordRequestsService.GetByTokenAsync(model.token)) is null)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.ForgotPasswordProcessRequest Error = No record found for this token");
                throw new Exception($"Invalid request.Please create new forgot password request.");
            }

            var forgotPasswordRequests = await _forgotPasswordRequestsService.GetByTokenAsync(model.token);

            if (forgotPasswordRequests?.expires_on < DateTime.UtcNow)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.ForgotPasswordProcessRequest Error = Token expired ");
                throw new Exception($"Expired request.Please create new forgot password request.");
            }

            if (!forgotPasswordRequests!.is_active)
            {
                _logger.LogError($"SparkApp.Controllers.AccountController.ForgotPasswordProcessRequest Error = Request deactivated ");
                throw new Exception($"Expired request.Please create new forgot password request.");
            }

            forgotPasswordRequests.completed = true;
            forgotPasswordRequests.is_active = false;

            await _forgotPasswordRequestsService.UpdateAsync(forgotPasswordRequests.Id!, forgotPasswordRequests);


            var user = await _usersService.GetAsync(forgotPasswordRequests.userId!);
            user!.password = _encryptionService.Encrypt(model.ResetForgotPassword.password);

            await _usersService.UpdateAsync(user.Id!, user);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>Your password has been changed. <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > Accounts </a></p>", NotificationType.me, user.Id);

            #endregion

            responseModel.Success = true;
            responseModel.Message = "Password changed.Please login with new password.";
            responseModel.Data = true;

            return Ok(responseModel);
        }




        private async Task<AuthenticateResponseModel> AuthorizeUser(User user)
        {

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.username));

            var userInRoles = await _userRolesService.GetByUserIdAsync(user.Id!);
            // Add roles as multiple claims
            foreach (var userInRole in userInRoles)
            {
                var role = await _rolesService.GetAsync(userInRole.RoleId!);

                claims.Add(new Claim(ClaimTypes.Role, role.name));
            }

            // Optionally add other app specific claims as needed
            claims.Add(new Claim("is_active", user.is_active.ToString()));

            #region Token Generation

            var token = _jwtService.GenerateJwtToken(user, claims.ToArray());
            var refreshToken = _jwtService.GenerateRefreshToken(user, IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString());

            await _usersService.AddRefreshToken(refreshToken);

            _usersService.RemoveExpiredRefreshToken(user.Id!, _config.RefreshTokenExpiryTimeDays);

            #endregion

            user.last_login = DateTime.UtcNow;
            await _usersService.UpdateAsync(user.Id!, user);

            setTokenCookie(refreshToken.token);

            return new AuthenticateResponseModel
            {
                token = token,
                refreshToken = refreshToken.token,
                expires = GetExpiryTimestamp(token),
                email_address = user.email_address,
                is_active = user.is_active,
                is_email_verified = user.is_email_verified,
                language = user.language,
                timezone = user.timezone,
                username = user.username,
                id = user.Id!,
                subscription = _subscriptionService.ConvertToSubscriptionV3ViewModel(_subscriptionService.GetSubscription(user.Id!)!),
            };
        }

        private async Task<SparkService.Models.File> AddProfilePicture(string userId, string firstName, string lastName)
        {
            SparkService.Models.File generatedProfilePictureFile = new SparkService.Models.File();

            #region Adding user profile Image           

            Guid newProfilePictureFileName = Guid.NewGuid();
            string newProfilePictureExtention = "jpg";
            var pathToSaveProfilePicture = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", userId!, "profile");

            if (!Directory.Exists(pathToSaveProfilePicture))
            {
                Directory.CreateDirectory(pathToSaveProfilePicture);
            }

            generatedProfilePictureFile.originalName = string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention);
            generatedProfilePictureFile.name = string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention);

            using (var image = ImageHandler.GenerateRactangle(firstName, lastName))
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
                generatedProfilePictureFile.query_original = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "original", string.Format("{0}.{1}", newProfilePictureFileName, newProfilePictureExtention));


                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "400x320")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "400x320"));
                }

                var fullPath400x320 = Path.Combine(pathToSaveProfilePicture, "400x320", string.Format("{0}_400x320.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 480, 320, 100L, fullPath400x320);
                generatedProfilePictureFile.path_480x320 = fullPath400x320;
                generatedProfilePictureFile.query_480x320 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "400x320", string.Format("{0}_400x320.{1}", newProfilePictureFileName, newProfilePictureExtention));


                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "300x300")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "300x300"));
                }
                var fullPath300x300 = Path.Combine(pathToSaveProfilePicture, "300x300", string.Format("{0}_300x300.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 300, 300, 100L, fullPath300x300);
                generatedProfilePictureFile.path_300x300 = fullPath300x300;
                generatedProfilePictureFile.query_300x300 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "300x300", string.Format("{0}_300x300.{1}", newProfilePictureFileName, newProfilePictureExtention));



                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "100x100")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "100x100"));
                }
                var fullPath100x100 = Path.Combine(pathToSaveProfilePicture, "100x100", string.Format("{0}_100x100.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 100, 100, 100L, fullPath100x100);
                generatedProfilePictureFile.path_100x100 = fullPath100x100;
                generatedProfilePictureFile.query_100x100 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "100x100", string.Format("{0}_100x100.{1}", newProfilePictureFileName, newProfilePictureExtention));



                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "32x32")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "32x32"));
                }
                var fullPath32x32 = Path.Combine(pathToSaveProfilePicture, "32x32", string.Format("{0}_32x32.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 32, 32, 100L, fullPath32x32);
                generatedProfilePictureFile.path_32x32 = fullPath32x32;
                generatedProfilePictureFile.query_32x32 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "32x32", string.Format("{0}_32x32.{1}", newProfilePictureFileName, newProfilePictureExtention));



                if (!Directory.Exists(Path.Combine(pathToSaveProfilePicture, "16x16")))
                {
                    Directory.CreateDirectory(Path.Combine(pathToSaveProfilePicture, "16x16"));
                }
                var fullPath16x16 = Path.Combine(pathToSaveProfilePicture, "16x16", string.Format("{0}_16x16.{1}", newProfilePictureFileName, newProfilePictureExtention));
                ImageHandler.Save((Bitmap)System.Drawing.Image.FromStream(image), 16, 16, 100L, fullPath16x16);
                generatedProfilePictureFile.path_16x16 = fullPath16x16;
                generatedProfilePictureFile.query_16x16 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "16x16", string.Format("{0}_16x16.{1}", newProfilePictureFileName, newProfilePictureExtention));

                generatedProfilePictureFile.size = ((image.ToArray().Length / 1024f) / 1024f);
            }

            generatedProfilePictureFile.type = "image/jpeg";
            generatedProfilePictureFile.created_at = DateTime.Now.ToUniversalTime();

            await _fileService.CreateAsync(generatedProfilePictureFile);

            #endregion

            return generatedProfilePictureFile;
        }

        private async Task<FacebookModel?> GetFacebookMe(string accessToken)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri("https://graph.facebook.com");
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"/me?fields=id,name,gender,email,birthday,picture{{url}}&access_token={accessToken}";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FacebookModel>(result);
        }

        private async Task<GooglePeopleModel?> GetGoogleMe(string accessToken)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri("https://people.googleapis.com");
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                     new AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"/v1/people/me?personFields=names,birthdays,genders,emailAddresses,photos";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<GooglePeopleModel>(result);
        }

        private void SplitFullName(string fullName, out string firstName, out string lastName)
        {
            firstName = string.Empty;
            lastName = string.Empty;
            if (!string.IsNullOrEmpty(fullName))
            {

                lastName = string.Empty; firstName = string.Empty;
                int splitIndex = fullName.LastIndexOf(' ');
                if (splitIndex >= 0)
                {
                    firstName = fullName.Substring(0, splitIndex);
                    lastName = fullName.Substring(splitIndex + 1);
                }
                else
                    firstName = fullName;
            }

        }

        private void setTokenCookie(string token)
        {
            // append cookie with refresh token to the http response
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private DateTime GetExpiryTimestamp(string accessToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                    return DateTime.MinValue;
                if (!accessToken.Contains("."))
                    return DateTime.MinValue;

                string[] parts = accessToken.Split('.');
                JwtTokenExpiryModel? payload = Newtonsoft.Json.JsonConvert.DeserializeObject<JwtTokenExpiryModel>(Base64UrlEncoder.Decode(parts[1].ToString()));
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(payload!.exp);
                return dateTimeOffset.LocalDateTime;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }

        }
    }
}
