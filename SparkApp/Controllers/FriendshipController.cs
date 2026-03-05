using Asp.Versioning;
using SparkApp.APIModel.Conversations;
using SparkApp.APIModel.Friendships;
using SparkApp.Hubs;
using SparkApp.Security.Policy;
using SparkApp.Services;
using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Bcpg;
using System.Security.Claims;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/friendship")]
    public class FriendshipController : Controller
    {
        private readonly ILogger<FriendshipController> _logger;
        private readonly AppSettings _config;
        private readonly IAuthorizationService _authorizationService;
        private readonly FriendshipsService _friendshipsService;
        private readonly UserNotificationService _userNotificationService;
        private readonly UsersService _usersService;
        private readonly ConversationService _conversationService;
        private readonly ChatHub _chatHub;

        public FriendshipController(ILogger<FriendshipController> logger, IAuthorizationService authorizationService, FriendshipsService friendshipsService, UserNotificationService userNotificationService, UsersService usersService, ConversationService conversationService, ChatHub chatHub, IOptions<AppSettings> config)
              => (_logger, _authorizationService, _friendshipsService, _userNotificationService, _usersService, _conversationService, _chatHub, _config) = (logger, authorizationService, friendshipsService, userNotificationService, usersService, conversationService, chatHub, config.Value);


        [MapToApiVersion(1)]
        [HttpGet("{userId}/friends/{page}/{pageSize}")]
        public async Task<ActionResult<ResponseModel<object>>> Get([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Friendships() { user_id = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var friendships = _friendshipsService.GetMyFriends(userId, page, pageSize);


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = friendships.Item2,
                Items = friendships.Item1,
                page = page,
                pageSize = pageSize
            }; 

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPost("friends/{userId}")]
        public async Task<ActionResult<ResponseModel<object>>> AddFriend(FriendAddRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            if (model.userId == model.friend.friend_id)
            {
                _logger.LogError($"SparkApp.Controllers.FriendshipController.AddFriend Error = Both user and friend cannot be same");
                throw new Exception($"Both user and friend cannot be same.");
            }

            var friend = _usersService.GetMemberById(model.friend.friend_id!);
            if (friend is null)
            {
                _logger.LogError($"SparkApp.Controllers.FriendshipController.AddFriend Error = Friend not found");
                throw new Exception($"Friend not found.");
            }

            var friendRequest = new Friendships()
            {
                friend_id = model.friend.friend_id,
                created_at = DateTime.UtcNow,
                user_id = model.userId!,
                status = FriendshipsStatus.Accepted.ToString()
            };

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, friendRequest, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            bool friendships = _friendshipsService.AreFriends(model.userId, model.friend.friend_id);
            if (friendships)
            {
                _logger.LogError($"SparkApp.Controllers.FriendshipController.AddFriend Error = Already both users are friend");
                var user = _usersService.GetMemberById(model.userId!);
                throw new Exception($"You are already in friendship with user  (#{user.username})");
            }


            await _friendshipsService.CreateAsync(friendRequest);

            #region Add Converation And Conversation Member

            var conversation = new Conversations()
            {
                created_at = DateTime.UtcNow,
                created_by = model.userId!,
                Subject = "Friends",
                Type = ConversationType.Direct.ToString().ToLower(),
            };

            await _conversationService.Create(conversation);

            var member1 = new ConversationMembers()
            {
                ConversationId = conversation.Id!,
                created_at = DateTime.UtcNow,
                UserId = model.userId!
            };

            await _conversationService.AddMember(member1);

            var member2 = new ConversationMembers()
            {
                ConversationId = conversation.Id!,
                created_at = DateTime.UtcNow,
                UserId = model.friend.friend_id!
            };

            await _conversationService.AddMember(member2);


            await _chatHub.JoinRoom(conversation.Id!);

            #endregion

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have added a freiend <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > My friends </a></p>", NotificationType.friendship, model.userId!);

            await _userNotificationService.SendNotificationForEvent($"<p>You were added as freiend <a href='{string.Format("{0}/accounts", _config.ClientUrl)}' > My friends </a></p>", NotificationType.friendship, model.friend.friend_id);

            #endregion


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = $"Congratulations you and your friend both are in friendship";

            return Ok(responseModel);
        }
    }
}
