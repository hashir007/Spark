using Asp.Versioning;
using SparkApp.APIModel.Conversations;
using SparkApp.APIModel.EmailMessage;
using SparkApp.APIModel.General;
using SparkApp.Hubs;
using SparkApp.Security.Policy;
using SparkApp.Services;
using SparkService.Models;
using SparkService.Services;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X509;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;

namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/email")]
    public class EmailMessageController : Controller
    {

        private readonly ILogger<EmailMessageController> _logger;
        private readonly AppSettings _config;
        private readonly IAuthorizationService _authorizationService;
        private readonly UsersService _usersService;
        private readonly EmailMessageService _emailMessageService;
        private readonly EmailHub _emailHub;
        private readonly UserNotificationService _userNotificationService;


        public EmailMessageController(ILogger<EmailMessageController> logger, UsersService usersService, EmailMessageService emailMessageService, EmailHub emailHub, IAuthorizationService authorizationService, UserNotificationService userNotificationService, IOptions<AppSettings> config)
          => (_logger, _usersService, _emailMessageService, _emailHub, _authorizationService, _userNotificationService, _config) = (logger, usersService, emailMessageService, emailHub, authorizationService, userNotificationService, config.Value);



        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/messges/send")]
        public async Task<ActionResult<ResponseModel<object>>> Create(EmailMessageCreateRequest emailMessageCreate)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.Create Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                return new BadRequestObjectResult(ModelState);
            }

            #region Blocked Condition

            bool doesItContainsBlockedRecipients = false;
            List<string> blockedRecipients = new List<string>();

            foreach (var item in emailMessageCreate.emailMessageCreate.recipients)
            {
                var isBlockedReceiver = await _usersService.GetBlockedListForMember(emailMessageCreate.userId, item);
                if (isBlockedReceiver is not null)
                {
                    doesItContainsBlockedRecipients = true;
                    var blockedUser = await _usersService.GetAsync(item);
                    blockedRecipients.Add(blockedUser!.username);
                }
            }

            if (doesItContainsBlockedRecipients)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.Create Error = recipients selected have blocked member.");
                throw new Exception($"Sending failed.You have added blocked recipients  [{string.Join(",", blockedRecipients)}].Please remove blocked members and re-send message again.");
            }

            #endregion

            var emailMessage = new EmailMessage();
            emailMessage.subject = Regex.Replace(emailMessageCreate.emailMessageCreate.subject, "<.*?>", String.Empty);
            emailMessage.content = emailMessageCreate.emailMessageCreate.content;
            emailMessage.created_at = DateTime.Now.ToUniversalTime();
            emailMessage.reply_to_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
            emailMessage.created_by = emailMessageCreate.userId!;
            emailMessage.status = EmailStatus.Pending.ToString();


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, emailMessage, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _emailMessageService.CreateEmailMessageAsync(emailMessage);

            var emailMessageSentFolders = await _emailMessageService.GetEmailMessageFoldersByNameAsync("Sent");
            var emailMessageInboxFolders = await _emailMessageService.GetEmailMessageFoldersByNameAsync("Inbox");

            foreach (var item in emailMessageCreate.emailMessageCreate.senders)
            {
                var emailMessageSenders = new EmailMessageSenders();
                emailMessageSenders.user_id = item;
                emailMessageSenders.email_message_id = emailMessage!.Id!;

                await _emailMessageService.CreateEmailMessageSendersAsync(emailMessageSenders);

                if (emailMessageCreate.emailMessageCreate.reply_to_message_id is not null)
                {
                    var emailMessageWithSentFolders = new EmailMessageWithFolders();
                    emailMessageWithSentFolders.user_id = item;
                    emailMessageWithSentFolders.email_message_folder_id = emailMessageSentFolders!.Id!;
                    emailMessageWithSentFolders.email_message_id = emailMessage!.Id!;
                    emailMessageWithSentFolders.reply_to_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
                    emailMessageWithSentFolders.created_at = DateTime.Now.ToUniversalTime();
                    await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithSentFolders);


                    var emailMessageWithInboxFolders = new EmailMessageWithFolders();
                    emailMessageWithInboxFolders.user_id = item;
                    emailMessageWithInboxFolders.email_message_folder_id = emailMessageInboxFolders!.Id!;
                    emailMessageWithInboxFolders.email_message_id = emailMessage!.Id!;
                    emailMessageWithInboxFolders.reply_to_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
                    emailMessageWithInboxFolders.is_read = true;
                    emailMessageWithInboxFolders.created_at = DateTime.Now.ToUniversalTime();
                    await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithInboxFolders);

                    var emailParent = await _emailMessageService.GetEmailMessageWithFoldersAsync(emailMessageCreate.emailMessageCreate.reply_to_message_id, emailMessageSentFolders!.Id!, item);

                    if (emailParent is null)
                    {
                        var emailMessageWithSentCopyFolders = new EmailMessageWithFolders();
                        emailMessageWithSentCopyFolders.user_id = item;
                        emailMessageWithSentCopyFolders.email_message_folder_id = emailMessageSentFolders!.Id!;
                        emailMessageWithSentCopyFolders.email_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
                        emailMessageWithSentCopyFolders.created_at = DateTime.Now.ToUniversalTime();
                        await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithSentCopyFolders);
                    }
                }
                else
                {
                    var emailMessageWithFolders = new EmailMessageWithFolders();
                    emailMessageWithFolders.user_id = item;
                    emailMessageWithFolders.email_message_folder_id = emailMessageSentFolders!.Id!;
                    emailMessageWithFolders.email_message_id = emailMessage!.Id!;
                    emailMessageWithFolders.created_at = DateTime.Now.ToUniversalTime();
                    await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithFolders);
                }


                // SENDING NOTIFICATION START

                await _emailHub.SendEmailNotification(item, _emailMessageService.GetEmailMessageByEmailId(emailMessage.Id!, emailMessageSentFolders!.Id!, item)!);

                //SENDING NOTIFICATION END

            }

            foreach (var item in emailMessageCreate.emailMessageCreate.recipients)
            {
                var emailMessageRecipients = new EmailMessageRecipients();
                emailMessageRecipients.user_id = item;
                emailMessageRecipients.email_message_id = emailMessage!.Id!;

                await _emailMessageService.CreateEmailMessageRecipientsAsync(emailMessageRecipients);

                if (emailMessageCreate.emailMessageCreate.reply_to_message_id is not null)
                {
                    var emailMessageWithSentFolders = new EmailMessageWithFolders();
                    emailMessageWithSentFolders.user_id = item;
                    emailMessageWithSentFolders.email_message_folder_id = emailMessageSentFolders!.Id!;
                    emailMessageWithSentFolders.email_message_id = emailMessage!.Id!;
                    emailMessageWithSentFolders.reply_to_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
                    emailMessageWithSentFolders.created_at = DateTime.Now.ToUniversalTime();
                    await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithSentFolders);

                    var emailMessageWithInboxFolders = new EmailMessageWithFolders();
                    emailMessageWithInboxFolders.user_id = item;
                    emailMessageWithInboxFolders.email_message_folder_id = emailMessageInboxFolders!.Id!;
                    emailMessageWithInboxFolders.email_message_id = emailMessage!.Id!;
                    emailMessageWithInboxFolders.reply_to_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
                    emailMessageWithInboxFolders.is_read = true;
                    emailMessageWithInboxFolders.created_at = DateTime.Now.ToUniversalTime();
                    await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithInboxFolders);

                    var emailParent = await _emailMessageService.GetEmailMessageWithFoldersAsync(emailMessageCreate.emailMessageCreate.reply_to_message_id, emailMessageInboxFolders!.Id!, item);

                    if (emailParent is null)
                    {
                        var emailMessageWithInboxCopyFolders = new EmailMessageWithFolders();
                        emailMessageWithInboxCopyFolders.user_id = item;
                        emailMessageWithInboxCopyFolders.email_message_folder_id = emailMessageInboxFolders!.Id!;
                        emailMessageWithInboxCopyFolders.email_message_id = emailMessageCreate.emailMessageCreate.reply_to_message_id;
                        emailMessageWithInboxCopyFolders.created_at = DateTime.Now.ToUniversalTime();
                        await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithInboxCopyFolders);
                    }
                }
                else
                {
                    var emailMessageWithFolders = new EmailMessageWithFolders();
                    emailMessageWithFolders.user_id = item;
                    emailMessageWithFolders.email_message_folder_id = emailMessageInboxFolders!.Id!;
                    emailMessageWithFolders.email_message_id = emailMessage!.Id!;
                    emailMessageWithFolders.created_at = DateTime.Now.ToUniversalTime();
                    await _emailMessageService.CreateEmailMessageWithFoldersAsync(emailMessageWithFolders);

                }

                // SENDING NOTIFICATION START

                await _emailHub.SendEmailNotification(item, _emailMessageService.GetEmailMessageByEmailId(emailMessage.Id!, emailMessageSentFolders!.Id!, item)!);

                //SENDING NOTIFICATION END
            }

            foreach (var item in emailMessageCreate.emailMessageCreate.attachments)
            {
                var emailMessageAttachments = new EmailMessageAttachments();
                emailMessageAttachments.link = item;
                emailMessageAttachments.email_message_id = emailMessage!.Id!;

                await _emailMessageService.CreateEmailMessageAttachmentsAsync(emailMessageAttachments);
            }

            #region Notification 

            foreach (var item in emailMessageCreate.emailMessageCreate.recipients)
            {
                await _userNotificationService.SendNotificationForEvent($"<p>You have received a message. <a href='{string.Format("{0}/dashboard", _config.ClientUrl)}' > My messages </a></p>", NotificationType.messages, item);
            }

            #endregion

            try
            {

                var emailMessageCurrent = await _emailMessageService.GetEmailMessageAsync(emailMessage.Id!);
                if (emailMessageCurrent != null)
                {
                    emailMessageCurrent.status = EmailStatus.Sent.ToString();
                    await _emailMessageService.UpdateEmailMessageAsync(emailMessageCurrent.Id!, emailMessageCurrent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.Create Error = {ex.Message.ToString()}");
            }

            // SENDING EMAIL STOP         


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = emailMessage;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}/folders")]
        public async Task<ActionResult<ResponseModel<object>>> GetFolders([FromRoute][BindRequired] string userId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new EmailMessageWithFolders() { user_id = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var folders = _emailMessageService.GetEmailMessageFoldersV2Async(userId);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = folders;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/folders")]
        public async Task<ActionResult<ResponseModel<object>>> AddFolder(AddFolderCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var emailSystemFolders = _emailMessageService.GetEmailSystemFolders();

            if (emailSystemFolders.Any(x => x.name.ToLower() == model.newFolder.name.ToLower()))
            {
                return new BadRequestObjectResult("Folder with this name already exists");
            }

            var folder = await _emailMessageService.GetEmailMessageFoldersAsync(model.newFolder.name, model.userId!);
            if (folder is not null)
            {
                return new BadRequestObjectResult("Folder with this name already exists");
            }         


            EmailMessageFolders emailMessageFolders = new EmailMessageFolders();
            emailMessageFolders.name = model.newFolder.name;
            emailMessageFolders.is_system = false;
            emailMessageFolders.created_by = model.userId!;

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, emailMessageFolders, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _emailMessageService.CreateEmailMessageFoldersAsync(emailMessageFolders);

            #region Notification 

            await _userNotificationService.SendNotificationForEvent($"<p>You have added new label. <a href='{string.Format("{0}/dashboard", _config.ClientUrl)}' > Messages Labels </a></p>", NotificationType.messages, model.userId);

            #endregion

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = emailMessageFolders;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/messges/{page:int}/{pageSize:int}")]
        public async Task<ActionResult<ResponseModel<object>>> GetMessages([FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize, [FromRoute][BindRequired] string userId, [FromBody] EmailMessageRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new EmailMessage() { created_by = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }


            var messages = _emailMessageService.GetEmailMessagesByUserIdAndFolderId(
                userId!,
                model.folderId,
                model.search,
                ((page - 1) * pageSize),
                pageSize);


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = _emailMessageService.GetTotalEmailMessageCount(userId!, model.folderId),
                Items = messages,
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("account/{userId}/folders/move")]
        public async Task<ActionResult<ResponseModel<object>>> FolderMove(EmailMessageMoveCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var currentUserId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (!ModelState.IsValid)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.Move Error = {Newtonsoft.Json.JsonConvert.SerializeObject(ModelState)}");
                return new BadRequestObjectResult(ModelState);
            }

            var emailMessageWithFolder = await _emailMessageService.GetEmailMessageWithFoldersAsync(model.EmailMessageMove.email_id, model.EmailMessageMove.from_folder_id, currentUserId!);

            if (emailMessageWithFolder != null)
            {

                var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, emailMessageWithFolder, Operations.Update);

                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }

                emailMessageWithFolder.email_message_folder_id = model.EmailMessageMove.to_folder_id;

                await _emailMessageService.UpdateEmailMessageWithFoldersAsync(emailMessageWithFolder.Id!, emailMessageWithFolder);
            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = model.EmailMessageMove;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/messges/purge")]
        public async Task<ActionResult<ResponseModel<object>>> PurgeEmailMessage([FromRoute][BindRequired] string userId, [FromBody][BindRequired] EmailMessagePurge model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();


            var emailMessage = await _emailMessageService.GetEmailMessageAsync(model.Id!);

            if (emailMessage is null)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.PurgeEmailMessage Error = Not Found");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _emailMessageService.GetEmailMessageWithFoldersByUserIdAsync(userId, model.Id!), Operations.Delete);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _emailMessageService.DeleteEmailMessageForUser(model.Id!, userId, model.folder_id);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                id = model.Id!,
                userId = userId,
                folderId = model.folder_id
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/folder/{folderId}/messges/{id}/read")]
        public async Task<ActionResult<ResponseModel<bool>>> MarkMessageRead([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string folderId, [FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var currentUserId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            var emailMessageWithFolder = await _emailMessageService.GetEmailMessageWithFoldersAsync(id, folderId, currentUserId!);

            if (emailMessageWithFolder is null)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.MarkMessageRead Error = NOT FOUND");
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, emailMessageWithFolder, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            emailMessageWithFolder.is_read = true;
            await _emailMessageService.UpdateEmailMessageWithFoldersAsync(emailMessageWithFolder.Id!, emailMessageWithFolder);


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                id = id!,
                folderId = folderId,
                userId = userId
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("account/{userId}/folder/{folderId}/messges/{id}/starred")]
        public async Task<ActionResult<ResponseModel<object>>> MarkMessageStarred([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string folderId, [FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var currentUserId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            var emailMessageWithFolder = await _emailMessageService.GetEmailMessageWithFoldersAsync(id, folderId, currentUserId!);

            if (emailMessageWithFolder is null)
            {
                _logger.LogError($"SparkApp.Controllers.EmailMessageController.MarkMessageStarred Error = NOT FOUND");
                return NotFound();
            }


            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, emailMessageWithFolder, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            emailMessageWithFolder.is_starred = !emailMessageWithFolder.is_starred;
            await _emailMessageService.UpdateEmailMessageWithFoldersAsync(emailMessageWithFolder.Id!, emailMessageWithFolder);


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                is_starred = emailMessageWithFolder.is_starred,
                userId = userId,
                folderId = folderId,
                id = id
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("account/{userId}/folders/{folderId}/move-folders")]
        public async Task<ActionResult<ResponseModel<object>>> GetMoveFolders([FromRoute][BindRequired] string userId, [FromRoute][BindRequired] string folderId)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new EmailMessageWithFolders() { user_id = userId }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<EmailMessageFoldersViewModel> moveFolders = new List<EmailMessageFoldersViewModel>();

            var folders = _emailMessageService.GetEmailMessageFoldersV2Async(userId);

            var selectedFolder = await _emailMessageService.GetEmailMessageFoldersAsync(folderId);

            if (selectedFolder!.name == "Inbox")
            {
                moveFolders.AddRange(folders.Where(x => x.name != "Sent" && x.name != "Pending" && x.name != "Inbox" && x.name != "Starred").ToList());
            }
            else if (selectedFolder.name == "Sent")
            {
                moveFolders.AddRange(folders.Where(x => x.name == "Inbox" | x.name == "Deleted" | x.name == "Archive" && x.name != "Starred").ToList());
            }
            else if (selectedFolder.name == "Pending")
            {
                moveFolders.AddRange(folders.Where(x => x.name == "Deleted" | x.name == "Archive" && x.name != "Starred").ToList());
            }
            else if (selectedFolder.name == "Deleted")
            {
                moveFolders.AddRange(folders.Where(x => x.name != "Sent" && x.name != "Pending" && x.name != "Inbox" && x.name != "Deleted" && x.name != "Starred").ToList());
            }
            else if (selectedFolder.name == "Archive")
            {
                moveFolders.AddRange(folders.Where(x => x.name != "Sent" && x.name != "Pending" && x.name != "Inbox" && x.name != "Archive" && x.name != "Starred").ToList());
            }
            else
            {
                moveFolders.AddRange(folders.Where(x => x.name == "Inbox" | x.name == "Deleted" | x.name == "Archive" && x.name != "Starred").ToList());
            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = moveFolders;

            return Ok(responseModel);
        }

    }
}
