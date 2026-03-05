using Asp.Versioning;
using SparkApp.APIModel.Administrators;
using SparkApp.APIModel.User;
using SparkApp.Helper;
using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebASparkApppi.Authorization;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/administrator")]
    public class AdministratorController : Controller
    {
        private readonly ILogger<AdministratorController> _logger;
        private readonly UsersService _usersService;
        private readonly JwtUtils _jwtService;
        private readonly EncryptionService _encryptionService;
        private readonly AppSettings _config;
        private readonly UserRolesService _userRolesService;
        private readonly RolesService _rolesService;

        public AdministratorController(ILogger<AdministratorController> logger, UsersService usersService, JwtUtils jwtService, EncryptionService encryptionService, UserRolesService userRolesService, RolesService rolesService, IOptions<AppSettings> config)
              => (_logger, _usersService, _jwtService, _encryptionService, _userRolesService, _rolesService, _config) = (logger, usersService, jwtService, encryptionService, userRolesService, rolesService, config.Value);


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("login")]
        public async Task<ActionResult<ResponseModel<AuthenticateResponseModel2>>> AdministratorLogin(AuthenticateRequestModel loginUser)
        {
            ResponseModel<AuthenticateResponseModel2> responseModel = new ResponseModel<AuthenticateResponseModel2>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.AdministratorController.AdministratorLogin Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                throw new Exception("Validation failed.Fields not valid.");
            }

            var user = _usersService.GetAsync(loginUser.UserName, _encryptionService.Encrypt(loginUser.Password));

            if (user is null)
            {
                _logger.LogError($"SparkApp.Controllers.AdministratorController.AdministratorLogin Error = Invalid login, username={loginUser.UserName}");
                throw new Exception("Invalid login. Please check username or password");
            }

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.username));

            var userInRoles = await _userRolesService.GetByUserIdAsync(user.Id!);

            var administratorRole = await _rolesService.GetByNameAsync("Administrator");

            if (!userInRoles.Any(x=>x.RoleId == administratorRole.Id))
            {
                throw new Exception("Invalid login. Please check username or password");
            }

            // Add roles as multiple claims
            foreach (var userInRole in userInRoles)
            {
                var role = await _rolesService.GetAsync(userInRole.RoleId!);

                claims.Add(new Claim(ClaimTypes.Role, role.name));
            }

            // Optionally add other app specific claims as needed
            claims.Add(new Claim("is_active", user.is_active.ToString()));

            var token = _jwtService.GenerateJwtToken(user, claims.ToArray());
            var refreshToken = _jwtService.GenerateRefreshToken(user, IPAddressHelper.GetRemoteHostIpAddressUsingRemoteIpAddress(Request.HttpContext).ToString());

            await _usersService.AddRefreshToken(refreshToken);

            _usersService.RemoveExpiredRefreshToken(user.Id!, _config.RefreshTokenExpiryTimeDays);

            setTokenCookie(refreshToken.token);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new AuthenticateResponseModel2()
            {
                token = token,
                refreshToken = refreshToken.token,
                is_active = user.is_active,
                email = user.email_address,
                username = user.username,
                id = user.Id!
            };
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ResponseModel<AuthenticateResponseModel2>>> Refresh(TokenRequestModel model)
        {
            ResponseModel<AuthenticateResponseModel2> responseModel = new ResponseModel<AuthenticateResponseModel2>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.AdministratorController.Refresh Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
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
            responseModel.Data = new AuthenticateResponseModel2()
            {
                token = token,
                refreshToken = refreshToken.token,
                email = user.email_address,
                username = user.username,
                is_active = user.is_active
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("revoke-token"), Authorize]
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

    }
}
