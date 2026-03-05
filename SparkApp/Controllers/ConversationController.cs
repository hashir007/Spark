using Asp.Versioning;
using SparkApp.APIModel.Conversations;
using SparkApp.APIModel.Member;
using SparkApp.Hubs;
using SparkApp.Security.Policy;
using SparkApp.Services;
using SparkService.Models;
using SparkService.Services;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using System.Drawing.Printing;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using static SparkService.ViewModels.AuthorizeNetSubscriptionRequest;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;

namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/conversation")]
    public class ConversationController : Controller
    {
        private readonly ILogger<ConversationController> _logger;
        private readonly AppSettings _config;
        private readonly UserNotificationService _userNotificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly UsersService _usersService;
        private readonly ConversationService _conversationService;
        private readonly ChatHub _chatHub;

        public ConversationController(ILogger<ConversationController> logger, IAuthorizationService authorizationService, UsersService usersService, ConversationService conversationService, ChatHub chatHub, UserNotificationService userNotificationService, IOptions<AppSettings> config)
           => (_logger, _authorizationService, _usersService, _conversationService, _chatHub, _userNotificationService, _config) = (logger, authorizationService, usersService, conversationService, chatHub, userNotificationService, config.Value);


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/create")]
        public async Task<ActionResult<ResponseModel<object>>> Create([FromRoute][BindRequired] string userId, [FromBody] ConversationCreate model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            var conversation = new Conversations()
            {
                created_at = DateTime.UtcNow,
                created_by = userId!,
                Subject = model.Subject,
                Type = model.Type
            };

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, conversation, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _conversationService.Create(conversation);

            var member = new ConversationMembers()
            {
                ConversationId = conversation.Id,
                created_at = DateTime.UtcNow,
                UserId = userId!
            };

            await _conversationService.AddMember(member);

            await _chatHub.JoinRoom(conversation.Id!);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = conversation;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/member")]
        public async Task<ActionResult<ResponseModel<object>>> AddMember([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            var conversation = _conversationService.GetConversation(id);

            if (conversation.type == "Direct")
            {
                var conversationMembers = await _conversationService.GetMembers(id);

                if (conversationMembers.Count() > 1)
                {
                    _logger.LogError($"SparkApp.Controllers.UserController.AddMember Error = Direct chat cannot have more than 2 members.");
                    throw new Exception("Direct chat cannot have more than 2 members.");
                }
            }

            var member = new ConversationMembers()
            {
                ConversationId = id,
                created_at = DateTime.UtcNow,
                UserId = userId
            };

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _conversationService.GetConversationV1(id), Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }


            await _conversationService.AddMember(member);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = member;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/messages")]
        public async Task<ActionResult<ResponseModel<object>>> CreateMessage([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromBody] MessageCreate model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }


            var message = new ConversationMessages()
            {
                ConversationId = id,
                created_at = DateTime.UtcNow,
                created_by = userId!,
                Text = model.Text,
                status = 1,
                reply_to_message_id = model.reply_to_message_id
            };

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, message, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _conversationService.CreateMessage(message);

            if (model.Files is not null)
            {
                if (model.Files.Count > 0)
                {

                    foreach (var file in model.Files)
                    {
                        var conversationFile = new ConversationFiles()
                        {
                            ConversationId = id,
                            Link = file,
                            MessageId = message.Id!
                        };

                        await _conversationService.AddFileToMessage(conversationFile);
                    }

                }
            }


            var conversationMembers = await _conversationService.GetMembers(id);

            foreach (var members in conversationMembers)
            {
                if (members.UserId != userId)
                {
                    ConversationMessageReadReceipt conversationMessageReadReceipt = new ConversationMessageReadReceipt();

                    conversationMessageReadReceipt.conversationId = id;
                    conversationMessageReadReceipt.messageId = message.Id!;
                    conversationMessageReadReceipt.userId = members.UserId;
                    conversationMessageReadReceipt.isRead = false;
                    conversationMessageReadReceipt.created_at = DateTime.UtcNow;

                    await _conversationService.CreateConversationMessageReadReceipt(conversationMessageReadReceipt);
                }
            }

            var cloneMessage = _conversationService.GetMessage(message.Id!);

            await _chatHub.SendMessageToGroupAsync(cloneMessage!, "ReceiveMessage");

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = message;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}")]
        public async Task<ActionResult<ResponseModel<object>>> Get([FromRoute][BindRequired] string userId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Conversations() { created_by = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var conversations = _conversationService.GetConversations(userId!);

            List<ConversationViewModel> blockListFiltered = new List<ConversationViewModel>();

            foreach (var conversation in conversations)
            {

                List<ConversationMemberViewModel> blockListFilteredMembers = new List<ConversationMemberViewModel>();

                if (conversation.type == "chat")
                {
                    foreach (var member in conversation.members)
                    {
                        if (userId != member.userId)
                        {
                            var isBlocked = await _usersService.GetBlockedListForMember(userId, member.userId);
                            if (isBlocked is not null)
                            {
                                continue;
                            }
                        }

                        blockListFilteredMembers.Add(member);
                    }

                    blockListFiltered.Add(conversation);
                    blockListFiltered[blockListFiltered.Count - 1].members = blockListFilteredMembers;

                }
                else if (conversation.type == "direct")
                {
                    var isBlocked = await _usersService.GetBlockedListForMember(userId, conversation.members.FirstOrDefault(x => x.userId != userId)!.userId);
                    if (isBlocked is not null)
                    {
                        continue;
                    }

                    blockListFiltered.Add(conversation);
                }
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = blockListFiltered;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}/{id}")]
        public async Task<ActionResult<ResponseModel<object>>> GetConversation([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var conversationMember = await _conversationService.GetMembers(id);

            if (conversationMember is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.GetConversation Error = Conversation Not Found");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, conversationMember.Where(x => x.UserId == userId).FirstOrDefault(), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var conversations = _conversationService.GetConversation(id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = conversations;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}/{id}/messages/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> Messages([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var conversationMember = await _conversationService.GetMembers(id);

            if (conversationMember is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.Messages Error =Conversation Not Found");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, conversationMember.Where(x => x.UserId == userId).FirstOrDefault(), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var messages = _conversationService.GetConversationMessages(userId, id, page, pageSize);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                ConversationId = id,
                Total = messages.Item2,
                Items = messages.Item1,
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/messages/{messageId}/read")]
        public async Task<ActionResult> MarkMessageRead([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromRoute][BindRequired] string messageId)
        {
            var result = await _conversationService.GetConversationMessage(messageId);

            if (result is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.MarkMessageRead Error =message Not Found");
                return NotFound();
            }

            var conversationMembers = await _conversationService.GetMembers(id);

            if (!conversationMembers.Any(x => x.UserId == userId))
            {
                return Forbid();
            }

            await _conversationService.UpdateConversationMessageReadReceiptMarkReadAsync(userId, messageId, id);

            var message = _conversationService.GetMessage(messageId);

            await _chatHub.SendMessageToGroupAsync(message!, "MessageRead");

            return Ok();
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/messages/{messageId}/read-all")]
        public async Task<ActionResult> MarkMessageReadAll([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromRoute][BindRequired] string messageId)
        {
            var result = await _conversationService.GetConversationMessage(messageId);

            if (result is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.MarkMessageRead Error =message Not Found");
                return NotFound();
            }

            var conversationMembers = await _conversationService.GetMembers(id);

            if (!conversationMembers.Any(x => x.UserId == userId))
            {
                return Forbid();
            }

            var messages = await _conversationService.UpdateConversationMessageReadReceiptMarkAllReadAsync(userId, messageId, id);

            foreach (var updatedMessage in messages)
            {
                var message = _conversationService.GetMessage(updatedMessage.messageId!);

                await _chatHub.SendMessageToGroupAsync(message!, "MessageRead");
            }

            return Ok();
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}/direct/{memberId}")]
        public async Task<ActionResult<ResponseModel<object>>> GetDirectConversationWithUser([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string memberId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var conversations = _conversationService.GetDirectConversationWithUser(userId!, memberId);

            if (conversations is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.GetDirectConversationWithUser Error = conversation Not Found");
                return NotFound();
            }

            var conversationMember = await _conversationService.GetMembers(conversations.id!);

            if (conversationMember is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.Messages Error =Conversation Not Found");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, conversationMember.Where(x => x.UserId == userId).FirstOrDefault(), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = conversations;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}/search/{term}")]
        public async Task<ActionResult<ResponseModel<object>>> Search([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string term)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Conversations() { created_by = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var searchResuts = _conversationService.SearchConversations(userId, term);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new { message = searchResuts.Item1, groupConversation = searchResuts.Item2, privateConversation = searchResuts.Item3 };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/search-message/{messageId}")]
        public async Task<ActionResult<ResponseModel<object>>> SearchMessage([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromRoute][BindRequired] string messageId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Conversations() { created_by = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var searchResults = await _conversationService.GetSearchMessage(userId, id, messageId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                ConversationId = id,
                Total = searchResults.Item2,
                Items = searchResults.Item1,
                page = 1,
                pageSize = searchResults.Item2
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/messages/{messageId}/edit")]
        public async Task<ActionResult> MessageEdit([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromRoute][BindRequired] string messageId, [FromBody] MessageCreate model)
        {
            var result = await _conversationService.GetConversationMessage(messageId);

            if (result is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.MessageEdit Error =message Not Found");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, result, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var message = await _conversationService.EditConversationMessage(userId, id, messageId, model.Text, model.Files);

            await _chatHub.SendMessageToGroupAsync(message!, "MessageEdited");

            return Ok();
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/{id}/messages/{messageId}/remove")]
        public async Task<ActionResult> MessageRemove([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string id, [FromRoute][BindRequired] string messageId)
        {
            var result = await _conversationService.GetConversationMessage(messageId);

            if (result is null)
            {
                _logger.LogError($"SparkApp.Controllers.ConversationController.RemoveConversationMessage Error =message Not Found");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, result, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _conversationService.RemoveConversationMessage(messageId);

            await _chatHub.SendObjectToGroupAsync(id, new { conversationId = id, messageId = messageId }, "MessageRemoved");

            return Ok();
        }

    }
}
