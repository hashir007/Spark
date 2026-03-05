using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using PayPal.v1.Invoices;
using PayPal.v1.Orders;
using PayPal.v1.Payments;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Xml.Linq;

namespace SparkService.Services
{
    public class EmailMessageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Profile> _profilesCollection;
        private readonly IMongoCollection<Models.File> _filesCollection;
        private readonly IMongoCollection<UserRoles> _userRolesCollection;
        private readonly IMongoCollection<Roles> _rolesCollection;
        private readonly IMongoCollection<EmailMessage> _emailMessageCollection;
        private readonly IMongoCollection<EmailMessageAttachments> _emailMessageAttachmentsCollection;
        private readonly IMongoCollection<EmailMessageFolders> _emailMessageFoldersCollection;
        private readonly IMongoCollection<EmailMessageRecipients> _emailMessageRecipientsCollection;
        private readonly IMongoCollection<EmailMessageSenders> _emailMessageSendersCollection;
        private readonly IMongoCollection<EmailMessageWithFolders> _emailMessageWithFoldersCollection;
        private readonly UsersService _usersService;

        public EmailMessageService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, IHttpContextAccessor httpContextAccessor, UsersService usersService)
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

            _emailMessageCollection = mongoDatabase.GetCollection<EmailMessage>(
               happySugarDaddyDatabaseSettings.Value.EmailMessageCollectionName);

            _emailMessageAttachmentsCollection = mongoDatabase.GetCollection<EmailMessageAttachments>(
              happySugarDaddyDatabaseSettings.Value.EmailMessageAttachmentsCollectionName);

            _emailMessageAttachmentsCollection = mongoDatabase.GetCollection<EmailMessageAttachments>(
             happySugarDaddyDatabaseSettings.Value.EmailMessageAttachmentsCollectionName);

            _emailMessageFoldersCollection = mongoDatabase.GetCollection<EmailMessageFolders>(
             happySugarDaddyDatabaseSettings.Value.EmailMessageFoldersCollectionName);

            _emailMessageRecipientsCollection = mongoDatabase.GetCollection<EmailMessageRecipients>(
             happySugarDaddyDatabaseSettings.Value.EmailMessageRecipientsCollectionName);

            _emailMessageSendersCollection = mongoDatabase.GetCollection<EmailMessageSenders>(
             happySugarDaddyDatabaseSettings.Value.EmailMessageSendersCollectionName);

            _emailMessageWithFoldersCollection = mongoDatabase.GetCollection<EmailMessageWithFolders>(
             happySugarDaddyDatabaseSettings.Value.EmailMessageWithFoldersCollectionName);

            _usersService = usersService;
        }


        public async Task<List<EmailMessage>> GetEmailMessageAsync() =>
            await _emailMessageCollection.Find(_ => true).ToListAsync();

        public async Task<EmailMessage?> GetEmailMessageAsync(string id) =>
            await _emailMessageCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateEmailMessageAsync(EmailMessage newBook) =>
            await _emailMessageCollection.InsertOneAsync(newBook);

        public async Task UpdateEmailMessageAsync(string id, EmailMessage updatedBook) =>
            await _emailMessageCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveEmailMessageAsync(string id) =>
            await _emailMessageCollection.DeleteOneAsync(x => x.Id == id);


        public async Task<List<EmailMessageAttachments>> GetEmailMessageAttachmentsAsync() =>
         await _emailMessageAttachmentsCollection.Find(_ => true).ToListAsync();

        public async Task<EmailMessageAttachments?> GetEmailMessageAttachmentsAsync(string id) =>
            await _emailMessageAttachmentsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateEmailMessageAttachmentsAsync(EmailMessageAttachments newBook) =>
            await _emailMessageAttachmentsCollection.InsertOneAsync(newBook);

        public async Task UpdateEmailMessageAttachmentsAsync(string id, EmailMessageAttachments updatedBook) =>
            await _emailMessageAttachmentsCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveEmailMessageAttachmentsAsync(string id) =>
            await _emailMessageAttachmentsCollection.DeleteOneAsync(x => x.Id == id);


        public async Task<List<EmailMessageFolders>> GetEmailMessageFoldersAsync() =>
            await _emailMessageFoldersCollection.Find(_ => true).ToListAsync();


        private IQueryable<EmailMessageViewModel> EmailMessages()
        {

            var messages = (
                        from m in _emailMessageCollection.AsQueryable()
                        join s in _emailMessageSendersCollection.AsQueryable() on m.Id equals s.email_message_id into senders
                        join r in _emailMessageRecipientsCollection.AsQueryable() on m.Id equals r.email_message_id into recipients
                        join a in _emailMessageAttachmentsCollection.AsQueryable() on m.Id equals a.email_message_id into attachments
                        from attachments_files in attachments.DefaultIfEmpty()
                        select new EmailMessageViewModel()
                        {
                            attachments = attachments.Select(x => new EmailMessageAttachmentsViewModel
                            {
                                id = x.Id,
                                link = x.link
                            }).ToList(),
                            content = m.content,
                            created_at = m.created_at,
                            id = m.Id,
                            subject = m.subject,
                            reply_to_message_id = m.reply_to_message_id,
                            updated_at = m.updated_at,
                            created_by = m.created_by,
                            recipients = recipients.Select(x => new EmailMessageRecipientsViewModel()
                            {
                                userId = x.user_id
                            }).ToList(),
                            senders = senders.Select(x => new EmailMessageSendersViewModel()
                            {
                                userId = x.user_id
                            }).ToList(),
                            status = m.status,
                        }).AsQueryable();


            return messages;
        }

        public List<EmailMessageFoldersViewModel> GetEmailMessageFoldersV2Async(string userId)
        {
            var folders = _emailMessageFoldersCollection.AsQueryable().Where(u => u.is_system == true && u.created_by == null)
                .Select(x => new EmailMessageFoldersViewModel
                {
                    id = x.Id,
                    name = x.name
                }).Union(_emailMessageFoldersCollection.AsQueryable().Where(x => x.created_by == userId && x.is_system == false)
            .Select(x => new EmailMessageFoldersViewModel
            {
                id = x.Id,
                name = x.name
            })).ToList();

            foreach (var folder in folders)
            {
                var emailWithFolders = _emailMessageWithFoldersCollection.AsQueryable()
                    .Where(x => x.user_id == userId && x.email_message_folder_id == folder.id).ToArray();

                folder.unreadcount = EmailMessages()
                    .Where(x => emailWithFolders.Where(x => x.is_read == false)
                    .Select(p => p.email_message_id).ToList().Contains(x.id) && x.reply_to_message_id == null).Count();
            }

            return folders;
        }

        public async Task<EmailMessageFolders?> GetEmailMessageFoldersAsync(string id) =>
            await _emailMessageFoldersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<EmailMessageFolders?> GetEmailMessageFoldersAsync(string name, string userId) =>
            await _emailMessageFoldersCollection.Find(x => x.name.ToLower() == name.ToLower() && x.created_by == userId).FirstOrDefaultAsync();

        public async Task<EmailMessageFolders?> GetEmailMessageFoldersByNameAsync(string name) =>
           await _emailMessageFoldersCollection.Find(x => x.name.ToLower() == name.ToLower()).FirstOrDefaultAsync();

        public async Task CreateEmailMessageFoldersAsync(EmailMessageFolders newBook) =>
            await _emailMessageFoldersCollection.InsertOneAsync(newBook);

        public async Task UpdateEmailMessageFoldersAsync(string id, EmailMessageFolders updatedBook) =>
            await _emailMessageFoldersCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveEmailMessageFoldersAsync(string id) =>
            await _emailMessageFoldersCollection.DeleteOneAsync(x => x.Id == id);


        public async Task<List<EmailMessageRecipients>> GetEmailMessageRecipientsAsync() =>
           await _emailMessageRecipientsCollection.Find(_ => true).ToListAsync();

        public async Task<EmailMessageRecipients?> GetEmailMessageRecipientsAsync(string id) =>
            await _emailMessageRecipientsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateEmailMessageRecipientsAsync(EmailMessageRecipients newBook) =>
            await _emailMessageRecipientsCollection.InsertOneAsync(newBook);

        public async Task UpdateEmailMessageRecipientsAsync(string id, EmailMessageRecipients updatedBook) =>
            await _emailMessageRecipientsCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveEmailMessageRecipientsAsync(string id) =>
            await _emailMessageRecipientsCollection.DeleteOneAsync(x => x.Id == id);


        public async Task<List<EmailMessageSenders>> GetEmailMessageSendersAsync() =>
           await _emailMessageSendersCollection.Find(_ => true).ToListAsync();

        public async Task<EmailMessageSenders?> GetEmailMessageSendersAsync(string id) =>
            await _emailMessageSendersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateEmailMessageSendersAsync(EmailMessageSenders newBook) =>
            await _emailMessageSendersCollection.InsertOneAsync(newBook);

        public async Task UpdateEmailMessageSendersAsync(string id, EmailMessageSenders updatedBook) =>
            await _emailMessageSendersCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveEmailMessageSendersAsync(string id) =>
            await _emailMessageSendersCollection.DeleteOneAsync(x => x.Id == id);

        public async Task<List<EmailMessageWithFolders>> GetEmailMessageWithFoldersAsync() =>
            await _emailMessageWithFoldersCollection.Find(_ => true).ToListAsync();

        public async Task<EmailMessageWithFolders?> GetEmailMessageWithFoldersAsync(string id) =>
            await _emailMessageWithFoldersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<EmailMessageWithFolders?> GetEmailMessageWithFoldersByUserIdAsync(string userId, string emailId) =>
            await _emailMessageWithFoldersCollection.Find(x => x.user_id == userId && x.email_message_id == emailId).FirstOrDefaultAsync();

        public async Task CreateEmailMessageWithFoldersAsync(EmailMessageWithFolders newBook) =>
            await _emailMessageWithFoldersCollection.InsertOneAsync(newBook);

        public async Task UpdateEmailMessageWithFoldersAsync(string id, EmailMessageWithFolders updatedBook) =>
            await _emailMessageWithFoldersCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveEmailMessageWithFoldersAsync(string id) =>
            await _emailMessageWithFoldersCollection.DeleteOneAsync(x => x.Id == id);



        public async Task<EmailMessageWithFolders?> GetEmailMessageWithFoldersAsync(string email_message_id, string email_message_folder_id, string userId) =>
            await _emailMessageWithFoldersCollection.Find(x => x.email_message_id == email_message_id && x.email_message_folder_id == email_message_folder_id && x.user_id == userId).FirstOrDefaultAsync();

        private List<EmailMessageViewModel> MapReplies(List<EmailMessageViewModel> emailMessages, string folderOrLablelId, string userId)
        {
            List<EmailMessageViewModel> messagesWithReplies = emailMessages;

            Action<EmailMessageViewModel>? FormatAndSetReplies = null;
            FormatAndSetReplies = (parent) =>
            {
                var children = (from c in EmailMessages().AsQueryable()
                                join d in _emailMessageWithFoldersCollection.AsQueryable()
                                on c.id equals d.email_message_id
                                where d.email_message_folder_id == folderOrLablelId
                                && d.user_id == userId
                                && d.reply_to_message_id == parent.id
                                select new EmailMessageViewModel
                                {
                                    attachments = c.attachments,
                                    content = c.content,
                                    created_at = c.created_at,
                                    created_by = c.created_by,
                                    created_by_user = c.created_by_user,
                                    id = c.id,
                                    recipients = c.recipients,
                                    reply_to_message = c.reply_to_message,
                                    reply_to_message_id = c.reply_to_message_id,
                                    senders = c.senders,
                                    status = c.status,
                                    subject = c.subject,
                                    updated_at = c.updated_at,
                                    is_read = d.is_read,
                                    is_starred = d.is_starred,
                                }).OrderByDescending(x => x.created_at.Date).
                                ThenByDescending(c => c.created_at.TimeOfDay).ToList();


                if (children.Any())
                {
                    children.ForEach(x => x.created_by_user = _usersService.GetMemberById(x.created_by!));
                    children.ForEach(x => x.senders.ForEach(x => x.user = _usersService.GetMemberById(x.userId!)));
                    children.ForEach(x => x.recipients.ForEach(x => x.user = _usersService.GetMemberById(x.userId!)));

                    parent.reply_to_message = children;
                }
                else
                {
                    return;
                }

                foreach (var child in children)
                {
                    FormatAndSetReplies(child);
                }
            };

            foreach (var messages in messagesWithReplies)
            {
                FormatAndSetReplies(messages);
            }

            return messagesWithReplies;
        }

        public int GetTotalEmailMessageCount(string userId, string folderOrLablelId)
        {
            int total = 0;

            var folder = _emailMessageFoldersCollection.Find(x => x.Id == folderOrLablelId).FirstOrDefault();
            var emailWithFolders = _emailMessageWithFoldersCollection.Find(x => x.user_id == userId && x.email_message_folder_id == folder.Id).ToList();

            total = EmailMessages().Where(x => emailWithFolders.Select(x => x.email_message_id).ToList().Contains(x.id!)).Count();

            return total;
        }

        public List<EmailMessageViewModel> GetEmailMessagesByUserIdAndFolderId(string userId, string folderId, string search, int skip, int limit)
        {
            List<EmailMessageViewModel> emailMessageViewModels = new List<EmailMessageViewModel>();

            var starredFolder = _emailMessageFoldersCollection.Find(x => x.name == "Starred").FirstOrDefault();

            var folder = _emailMessageFoldersCollection.Find(x => x.Id == folderId).FirstOrDefault();

            if (starredFolder.Id == folderId)
            {
                emailMessageViewModels = (from c in EmailMessages().AsQueryable()
                                          join d in _emailMessageWithFoldersCollection.AsQueryable()
                                          on c.id equals d.email_message_id
                                          where (d.is_starred == true
                                           && d.user_id == userId
                                           && (c.content.ToLower().Contains(search.ToLower()) || c.subject.ToLower().Contains(search.ToLower())))
                                           && d.reply_to_message_id == null
                                          select new EmailMessageViewModel
                                          {
                                              attachments = c.attachments,
                                              content = c.content,
                                              created_at = c.created_at,
                                              created_by = c.created_by,
                                              created_by_user = c.created_by_user,
                                              id = c.id,
                                              recipients = c.recipients,
                                              reply_to_message = c.reply_to_message,
                                              reply_to_message_id = c.reply_to_message_id,
                                              senders = c.senders,
                                              status = c.status,
                                              subject = c.subject,
                                              updated_at = c.updated_at,
                                              is_read = d.is_read,
                                              is_starred = d.is_starred,
                                          }).OrderByDescending(x => x.created_at.Date)
                      .ThenByDescending(c => c.created_at.TimeOfDay).Skip(skip).Take(limit).ToList();
            }
            else
            {
                emailMessageViewModels = (from c in EmailMessages().AsQueryable()
                                          join d in _emailMessageWithFoldersCollection.AsQueryable()
                                          on c.id equals d.email_message_id
                                          where (d.email_message_folder_id == folder.Id
                                          && d.user_id == userId
                                          && (c.content.ToLower().Contains(search.ToLower()) || c.subject.ToLower().Contains(search.ToLower())))
                                          && d.reply_to_message_id == null
                                          select new EmailMessageViewModel
                                          {
                                              attachments = c.attachments,
                                              content = c.content,
                                              created_at = c.created_at,
                                              created_by = c.created_by,
                                              created_by_user = c.created_by_user,
                                              id = c.id,
                                              recipients = c.recipients,
                                              reply_to_message = c.reply_to_message,
                                              reply_to_message_id = c.reply_to_message_id,
                                              senders = c.senders,
                                              status = c.status,
                                              subject = c.subject,
                                              updated_at = c.updated_at,
                                              is_read = d.is_read,
                                              is_starred = d.is_starred,
                                          }).OrderByDescending(x => x.created_at.Date)
                    .ThenByDescending(c => c.created_at.TimeOfDay).Skip(skip).Take(limit).ToList();
            }

            emailMessageViewModels.ForEach(x => x.created_by_user = _usersService.GetMemberById(x.created_by!));
            emailMessageViewModels.ForEach(x => x.senders.ForEach(x => x.user = _usersService.GetMemberById(x.userId!)));
            emailMessageViewModels.ForEach(x => x.recipients.ForEach(x => x.user = _usersService.GetMemberById(x.userId!)));

            if (emailMessageViewModels.Any())
            {
                emailMessageViewModels = MapReplies(emailMessageViewModels, folderId, userId);
            }

            return emailMessageViewModels;
        }

        public EmailMessageViewModel? GetEmailMessageByEmailId(string id, string folderId, string userId)
        {
            var email = EmailMessages().Where(x => x.id == id).FirstOrDefault();

            email.created_by_user = _usersService.GetMemberById(email.created_by!);
            email.senders.ForEach(x => x.user = _usersService.GetMemberById(x.userId!));
            email.recipients.ForEach(x => x.user = _usersService.GetMemberById(x.userId!));

            return MapReplies(new List<EmailMessageViewModel>
                {
                    email
                }, folderId, userId).FirstOrDefault();
        }

        public async Task DeleteEmailMessage(string id)
        {
            var email = await _emailMessageCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

            if (email is not null)
            {
                var emailMessageWithFolders = _emailMessageWithFoldersCollection.Find(x => x.email_message_id == email.Id).ToList();

                foreach (var folder in emailMessageWithFolders)
                {
                    await _emailMessageWithFoldersCollection.DeleteOneAsync(x => x.Id == folder.Id);
                }

                var emailMessageSenders = _emailMessageSendersCollection.Find(x => x.email_message_id == email.Id).ToList();

                foreach (var sender in emailMessageSenders)
                {
                    await _emailMessageSendersCollection.DeleteOneAsync(x => x.Id == sender.Id);
                }

                var emailMessageRecipients = _emailMessageRecipientsCollection.Find(x => x.email_message_id == email.Id).ToList();

                foreach (var recipient in emailMessageRecipients)
                {
                    await _emailMessageRecipientsCollection.DeleteOneAsync(x => x.Id == recipient.Id);
                }

                var emailMessageAttachments = _emailMessageAttachmentsCollection.Find(x => x.email_message_id == email.Id).ToList();

                foreach (var attachment in emailMessageAttachments)
                {
                    await _emailMessageAttachmentsCollection.DeleteOneAsync(x => x.Id == attachment.Id);
                }

                await _emailMessageCollection.DeleteOneAsync(x => x.Id == email.Id);
            }
        }

        public async Task DeleteEmailMessageForUser(string emailId, string userId, string folderId)
        {
            await _emailMessageWithFoldersCollection.DeleteOneAsync(x => x.email_message_id == emailId && x.user_id == userId && x.email_message_folder_id == folderId);
        }

        public List<EmailMessageFolders> GetEmailSystemFolders()
        {
            return _emailMessageFoldersCollection.AsQueryable().Where(x => x.is_system).ToList();
        }

    }

}
