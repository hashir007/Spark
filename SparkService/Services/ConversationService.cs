using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using static SparkService.ViewModels.AuthorizeNetSubscriptionRequest;

namespace SparkService.Services
{
    public class ConversationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Models.File> _filesCollection;
        private readonly IMongoCollection<ConversationFiles> _conversationFilesCollection;
        private readonly IMongoCollection<ConversationMembers> _conversationMembersCollection;
        private readonly IMongoCollection<Conversations> _conversationsCollection;
        private readonly IMongoCollection<ConversationMessages> _conversationMessagesCollection;
        private readonly IMongoCollection<ConversationMessageReadReceipt> _conversationMessageReadReceiptCollection;
        private readonly IMongoCollection<ConversationMessageEditedReceipt> _conversationMessageEditedReceiptCollection;
        private readonly IMongoCollection<ConversationMessageDeletedReceipt> _conversationMessageDeletedReceiptCollection;
        private readonly UsersService _usersService;

        public ConversationService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, IHttpContextAccessor httpContextAccessor, UsersService usersService)
        {

            _httpContextAccessor = httpContextAccessor;

            var mongoClient = new MongoClient(
           happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);


            _usersCollection = mongoDatabase.GetCollection<User>(
               happySugarDaddyDatabaseSettings.Value.UsersCollectionName);


            _filesCollection = mongoDatabase.GetCollection<Models.File>(
              happySugarDaddyDatabaseSettings.Value.FileCollectionName);


            _conversationFilesCollection = mongoDatabase.GetCollection<Models.ConversationFiles>(
              happySugarDaddyDatabaseSettings.Value.ConversationFilesCollectionName);


            _conversationMembersCollection = mongoDatabase.GetCollection<Models.ConversationMembers>(
              happySugarDaddyDatabaseSettings.Value.ConversationMembersCollectionName);


            _conversationsCollection = mongoDatabase.GetCollection<Models.Conversations>(
              happySugarDaddyDatabaseSettings.Value.ConversationsCollectionName);


            _conversationMessagesCollection = mongoDatabase.GetCollection<Models.ConversationMessages>(
              happySugarDaddyDatabaseSettings.Value.ConversationMessagesCollectionName);

            _conversationMessageReadReceiptCollection = mongoDatabase.GetCollection<Models.ConversationMessageReadReceipt>(
              happySugarDaddyDatabaseSettings.Value.ConversationMessageReadReceiptCollectionName);


            _conversationMessageEditedReceiptCollection = mongoDatabase.GetCollection<Models.ConversationMessageEditedReceipt>(
             happySugarDaddyDatabaseSettings.Value.ConversationMessageEditedReceiptCollectionName);

            _conversationMessageDeletedReceiptCollection = mongoDatabase.GetCollection<Models.ConversationMessageDeletedReceipt>(
              happySugarDaddyDatabaseSettings.Value.ConversationMessageDeletedReceiptCollectionName);

            _usersService = usersService;

        }

        public async Task Create(Conversations conversations) =>
             await _conversationsCollection.InsertOneAsync(conversations);

        public async Task<Conversations> GetConversationV1(string id) => await _conversationsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task AddMember(ConversationMembers conversationMembers) =>
             await _conversationMembersCollection.InsertOneAsync(conversationMembers);

        public async Task CreateMessage(ConversationMessages messages) =>
             await _conversationMessagesCollection.InsertOneAsync(messages);

        public async Task AddFileToMessage(ConversationFiles file) =>
            await _conversationFilesCollection.InsertOneAsync(file);

        public async Task UpdateConversationAsync(string id, Conversations updateConversation) =>
           await _conversationsCollection.ReplaceOneAsync(x => x.Id == id, updateConversation);

        public async Task<ConversationMessages?> GetConversationMessage(string Id) => await _conversationMessagesCollection.Find(x => x.Id == Id).FirstOrDefaultAsync();

        private int GetConversationUnreadCount(string userId, string conversationId)
        {
            return _conversationMessageReadReceiptCollection.AsQueryable().Where(x => x.conversationId == conversationId && x.userId == userId && x.isRead == false).Count();
        }

        private ConversationMessageViewModel? GetConversationLastUnreadMessage(string userId, string conversationId)
        {
            var lastUnReadMessage = (from m in _conversationMessagesCollection.AsQueryable()
                                     join r in _conversationMessageReadReceiptCollection.AsQueryable()
                                     on m.Id equals r.messageId
                                     where
                                     r.conversationId == conversationId
                                     && m.status == 1
                                     && r.userId == userId
                                     && r.isRead == false
                                     select r).OrderByDescending(x => x.created_at.Date).
                            ThenByDescending(c => c.created_at.TimeOfDay).FirstOrDefault();

            if (lastUnReadMessage is null)
            {
                return null;
            }

            return GetMessage(lastUnReadMessage.messageId);
        }

        public IEnumerable<ConversationViewModel> GetConversations(string userId)
        {

            var participatedConversation = (from cm in _conversationMembersCollection.AsQueryable() where cm.UserId == userId select cm.ConversationId).ToList();


            var result = (from c in _conversationsCollection.AsQueryable()
                          join cm in _conversationMembersCollection.AsQueryable() on c.Id equals cm.ConversationId into members
                          where participatedConversation.Contains(c.Id)
                          select new ConversationViewModel
                          {
                              members = members.Select(x => new ConversationMemberViewModel
                              {
                                  conversationId = x.ConversationId,
                                  created_at = x.created_at,
                                  id = x.Id,
                                  userId = x.UserId
                              }).ToList(),
                              created_at = c.created_at,
                              created_by = c.created_by,
                              id = c.Id,
                              subject = c.Subject,
                              type = c.Type
                          }).ToList();


            foreach (ConversationViewModel item in result)
            {
                item.created_by_user = _usersService.GetDetailedV4(item.created_by!);
                item.members.ForEach(z => z.user = _usersService.GetDetailedV4(z.userId));
                item.unreadCount = GetConversationUnreadCount(userId, item.id!);
                item.unread = item.unreadCount > 0 ? true : false;
                item.lastUnreadMessage = GetConversationLastUnreadMessage(userId, item.id!);
            }

            return result;
        }

        public (IEnumerable<ConversationMessageViewModel>, int) GetConversationMessages(string userId, string conversationId, int page, int pageSize)
        {

            var members = Task.Run(async () => await _conversationMembersCollection.Find(x => x.ConversationId == conversationId).ToListAsync()).Result;

            List<string> conversationMembers = new List<string>();

            foreach (var member in members)
            {
                if (member.UserId != userId)
                {
                    var isBlocked = Task.Run(async () => await _usersService.GetBlockedListForMember(member.UserId, userId));
                    if (isBlocked is null)
                    {
                        conversationMembers.Add(member.UserId);
                    }
                }
                else
                {
                    conversationMembers.Add(member.UserId);
                }
            }


            var query = (from m in _conversationMessagesCollection.AsQueryable()
                         join r in _conversationMessageReadReceiptCollection.AsQueryable()
                         on m.Id equals r.messageId into readReceipts
                         where m.ConversationId == conversationId && m.status == 1
                         select new ConversationMessageViewModel
                         {
                             conversationId = m.ConversationId,
                             created_at = m.created_at,
                             created_by = m.created_by,
                             id = m.Id,
                             text = m.Text,
                             reply_to_message_id = m.reply_to_message_id,
                             readReceipt = readReceipts.Select(x => new ConversationMessageReadReceiptViewModel
                             {
                                 created_at = x.created_at,
                                 isRead = x.isRead,
                                 userId = x.userId,
                             }).ToList(),
                         }).AsQueryable();


            var result = query.OrderByDescending(x => x.created_at.Date).
                                ThenByDescending(c => c.created_at.TimeOfDay).Skip(((page - 1) * pageSize)).Take(pageSize).ToList();

            foreach (ConversationMessageViewModel item in result)
            {
                item.conversations = GetConversation(item.conversationId);

                item.conversations.unreadCount = GetConversationUnreadCount(item.created_by, item.id!);

                item.conversations.unread = item.conversations.unreadCount > 0 ? true : false;

                item.readReceipt.ForEach(p => p.member = _usersService.GetDetailedV4(p.userId));

                item.files = (from cf in _conversationFilesCollection.AsQueryable()
                              where cf.MessageId == item.id
                              select new ConversationFileViewModel
                              {
                                  id = cf.Id,
                                  link = cf.Link
                              }).ToList();

                item.created_by_user = _usersService.GetDetailedV4(item.created_by);

                item.editReceipt = (from er in _conversationMessageEditedReceiptCollection.AsQueryable()
                                    where er.messageId == item.id
                                    select new ConversationMessageEditedReceiptViewModel
                                    {
                                        created_at = er.created_at,
                                        userId = er.userId,
                                    }).OrderByDescending(x => x.created_at.Date).
                                ThenByDescending(c => c.created_at.TimeOfDay).FirstOrDefault();

                if (item.editReceipt is not null)
                {
                    item.editReceipt.user = _usersService.GetDetailedV4(item.editReceipt.userId);
                }

                if (item.reply_to_message_id is not null)
                {
                    item.reply_to_message = GetMessage(item.reply_to_message_id);
                }
            }

            result.Reverse();

            return (result, query.Count());
        }

        public ConversationMessageViewModel? GetMessage(string Id)
        {
            var query = (from m in _conversationMessagesCollection.AsQueryable()
                         join r in _conversationMessageReadReceiptCollection.AsQueryable()
                         on m.Id equals r.messageId into readReceipt
                         where m.Id == Id
                         select new ConversationMessageViewModel
                         {
                             conversationId = m.ConversationId,
                             created_at = m.created_at,
                             created_by = m.created_by,
                             id = m.Id,
                             text = m.Text,
                             reply_to_message_id = m.reply_to_message_id,
                             readReceipt = readReceipt.Select(x => new ConversationMessageReadReceiptViewModel
                             {
                                 isRead = x.isRead,
                                 created_at = x.created_at,
                                 userId = x.userId
                             }).ToList()
                         }).AsQueryable();

            var result = query.FirstOrDefault();

            if (result is null)
            {
                return result;
            }

            result!.conversations = GetConversation(result!.conversationId);

            result!.conversations.unreadCount = GetConversationUnreadCount(result!.created_by, result!.conversationId);
            result!.conversations.unread = result!.conversations.unreadCount > 0 ? true : false;
            result!.readReceipt.ForEach(p => p.member = _usersService.GetDetailedV4(p.userId));

            result!.files = (from cf in _conversationFilesCollection.AsQueryable()
                             where cf.MessageId == result.id
                             select new ConversationFileViewModel
                             {
                                 id = cf.Id,
                                 link = cf.Link
                             }).ToList();

            result.editReceipt = (from er in _conversationMessageEditedReceiptCollection.AsQueryable()
                                  where er.messageId == result.id
                                  select new ConversationMessageEditedReceiptViewModel
                                  {
                                      created_at = er.created_at,
                                      userId = er.userId
                                  }).OrderByDescending(x => x.created_at.Date).
                                ThenByDescending(c => c.created_at.TimeOfDay).FirstOrDefault();

            if (result.editReceipt is not null)
            {
                result.editReceipt.user = _usersService.GetDetailedV4(result.editReceipt.userId);
            }

            if (result.reply_to_message_id is not null)
            {
                result.reply_to_message = GetMessage(result.reply_to_message_id);
            }

            result!.created_by_user = _usersService.GetDetailedV4(result.created_by);

            return result;
        }

        public ConversationViewModel GetConversation(string Id)
        {

            var result = (from c in _conversationsCollection.AsQueryable()
                          join cm in _conversationMembersCollection.AsQueryable() on c.Id equals cm.ConversationId into members
                          where c.Id == Id
                          select new ConversationViewModel
                          {
                              members = members.Select(x => new ConversationMemberViewModel
                              {
                                  conversationId = x.ConversationId,
                                  created_at = x.created_at,
                                  id = x.Id,
                                  userId = x.UserId
                              }).ToList(),
                              created_at = c.created_at,
                              created_by = c.created_by,
                              id = c.Id,
                              subject = c.Subject,
                              type = c.Type
                          }).FirstOrDefault();

            result!.created_by_user = _usersService.GetDetailedV4(result.created_by!);
            result.members.ForEach(z => z.user = _usersService.GetDetailedV4(z.userId));

            return result;
        }

        public ConversationViewModel GetConversation(string userId, string Id)
        {

            var result = (from c in _conversationsCollection.AsQueryable()
                          join cm in _conversationMembersCollection.AsQueryable() on c.Id equals cm.ConversationId into members
                          where c.Id == Id && c.created_by == userId
                          select new ConversationViewModel
                          {
                              members = members.Select(x => new ConversationMemberViewModel
                              {
                                  conversationId = x.ConversationId,
                                  created_at = x.created_at,
                                  id = x.Id,
                                  userId = x.UserId
                              }).ToList(),
                              created_at = c.created_at,
                              created_by = c.created_by,
                              id = c.Id,
                              subject = c.Subject,
                              type = c.Type
                          }).FirstOrDefault();

            result!.created_by_user = _usersService.GetDetailedV4(result.created_by!);
            result.members.ForEach(z => z.user = _usersService.GetDetailedV4(z.userId));
            result.lastUnreadMessage = GetConversationLastUnreadMessage(userId, Id);

            return result;
        }

        public async Task<IEnumerable<ConversationMembers>> GetMembers(string Id) => await _conversationMembersCollection.Find(x => x.ConversationId == Id).ToListAsync();

        public async Task UpdateMessageAsync(string id, ConversationMessages model) =>
           await _conversationMessagesCollection.ReplaceOneAsync(x => x.Id == id, model);

        public ConversationViewModel? GetDirectConversationWithUser(string userId, string memberId)
        {
            var participants = new string[] { userId, memberId };

            var conv = _conversationMembersCollection.AsQueryable().GroupBy(x => x.ConversationId).
                Where(grp => grp.All(x => participants.Contains(x.UserId))).Select(x => x.Key).ToList();


            var result = (from c in _conversationsCollection.AsQueryable()
                          join cm in _conversationMembersCollection.AsQueryable() on c.Id equals cm.ConversationId into members
                          where conv.Contains(c.Id) && c.Type == ConversationType.Direct.ToString()
                          select new ConversationViewModel
                          {
                              members = members.Select(x => new ConversationMemberViewModel
                              {
                                  conversationId = x.ConversationId,
                                  created_at = x.created_at,
                                  id = x.Id,
                                  userId = x.UserId
                              }).ToList(),
                              created_at = c.created_at,
                              created_by = c.created_by,
                              id = c.Id,
                              subject = c.Subject,
                              type = c.Type
                          }).FirstOrDefault();

            if (result is not null)
            {
                result.created_by_user = _usersService.GetDetailedV4(result.created_by!);
                result.members.ForEach(z => z.user = _usersService.GetDetailedV4(z.userId)!);
                result.unreadCount = GetConversationUnreadCount(userId, result.id!);
                result.unread = result.unreadCount > 0 ? true : false;
                result.lastUnreadMessage = GetConversationLastUnreadMessage(userId, result.id!);
            }

            return result;
        }

        public async Task CreateConversationMessageReadReceipt(ConversationMessageReadReceipt conversationMessageReadReceipt) =>
             await _conversationMessageReadReceiptCollection.InsertOneAsync(conversationMessageReadReceipt);

        public ConversationMessageReadReceipt? GetConversationMessageReadReceipt(string messageId, string memberId, string conversationId) => _conversationMessageReadReceiptCollection.AsQueryable().Where(x => x.messageId == messageId && x.userId == memberId && x.conversationId == conversationId).FirstOrDefault();

        public async Task<List<ConversationMessageReadReceipt>> UpdateConversationMessageReadReceiptMarkAllReadAsync(string userId, string id, string conversationId)
        {
            List<ConversationMessageReadReceipt> unreadMessagesBeforeCurrentMessage = new List<ConversationMessageReadReceipt>();

            var currentMessage = _conversationMessageReadReceiptCollection.AsQueryable()
              .Where(x => x.messageId == id && x.conversationId == conversationId).FirstOrDefault();

            if (currentMessage is null)
            {
                return unreadMessagesBeforeCurrentMessage;
            }

            unreadMessagesBeforeCurrentMessage = _conversationMessageReadReceiptCollection.AsQueryable()
               .Where(x => x.isRead == false && x.conversationId == conversationId && x.userId == userId && x.created_at <= currentMessage.created_at).ToList();

            foreach (var item in unreadMessagesBeforeCurrentMessage)
            {
                item.isRead = true;
                await _conversationMessageReadReceiptCollection.ReplaceOneAsync(x => x.Id == item.Id, item);
            }

            return unreadMessagesBeforeCurrentMessage;
        }

        public async Task UpdateConversationMessageReadReceiptMarkReadAsync(string userId, string id, string conversationId)
        {
            var currentMessage = _conversationMessageReadReceiptCollection.AsQueryable()
            .Where(x => x.messageId == id && x.conversationId == conversationId).FirstOrDefault();

            if (currentMessage is null)
            {
                return;
            }

            currentMessage.isRead = true;
            await _conversationMessageReadReceiptCollection.ReplaceOneAsync(x => x.Id == currentMessage.Id, currentMessage);
        }

        public (List<ConversationSearchResultViewModel>, List<ConversationViewModel>, List<ConversationViewModel>) SearchConversations(string userId, string term)
        {
            List<ConversationSearchResultViewModel> conversationSearchResultViewModels = new List<ConversationSearchResultViewModel>();

            List<ConversationViewModel> conversationChatSearchResult = new List<ConversationViewModel>();

            List<ConversationViewModel> conversationDirectSearchResult = new List<ConversationViewModel>();

            var myConversations = GetConversations(userId);

            if (myConversations.Count() == 0)
            {
                return (conversationSearchResultViewModels, conversationChatSearchResult, conversationDirectSearchResult);
            }

            var query = (from m in _conversationMessagesCollection.AsQueryable()
                         join r in _conversationMessageReadReceiptCollection.AsQueryable()
                         on m.Id equals r.messageId into readReceipt
                         where myConversations.Any(x => x.id == m.ConversationId) && m.status == 1
                         select new ConversationMessageViewModel
                         {
                             conversationId = m.ConversationId,
                             created_at = m.created_at,
                             created_by = m.created_by,
                             id = m.Id,
                             text = m.Text,
                             readReceipt = readReceipt.Select(x => new ConversationMessageReadReceiptViewModel
                             {
                                 isRead = x.isRead,
                                 created_at = x.created_at,
                                 userId = x.userId
                             }).ToList()
                         }).AsQueryable();



            var result = query.Where(x => x.text.ToLower().Contains(term.ToLower()))
                .OrderByDescending(x => x.created_at.Date).ThenByDescending(c => c.created_at.TimeOfDay).ToList();


            foreach (ConversationMessageViewModel item in result)
            {
                item.conversations = GetConversation(item.conversationId);

                item.conversations.unreadCount = GetConversationUnreadCount(item.created_by, item.id!);

                item.conversations.unread = item.conversations.unreadCount > 0 ? true : false;

                item.readReceipt.ForEach(p => p.member = _usersService.GetDetailedV4(p.userId));

                item.files = (from cf in _conversationFilesCollection.AsQueryable()
                              where cf.MessageId == item.id
                              select new ConversationFileViewModel
                              {
                                  id = cf.Id,
                                  link = cf.Link
                              }).ToList();

                item.created_by_user = _usersService.GetDetailedV4(item.created_by);

                item.editReceipt = (from er in _conversationMessageEditedReceiptCollection.AsQueryable()
                                    where er.messageId == item.id
                                    select new ConversationMessageEditedReceiptViewModel
                                    {
                                        created_at = er.created_at,
                                        userId = er.userId,
                                    }).OrderByDescending(x => x.created_at.Date).
                                ThenByDescending(c => c.created_at.TimeOfDay).FirstOrDefault();

                if (item.editReceipt is not null)
                {
                    item.editReceipt.user = _usersService.GetDetailedV4(item.editReceipt.userId);
                }
            }

            result.Reverse();

            foreach (var message in result)
            {
                ConversationSearchResultViewModel conversationSearchResultViewModel = new ConversationSearchResultViewModel();

                conversationSearchResultViewModel.members = message.conversations.members;
                conversationSearchResultViewModel.created_at = message.conversations.created_at;
                conversationSearchResultViewModel.created_by = message.conversations.created_by;
                conversationSearchResultViewModel.subject = message.conversations.subject;
                conversationSearchResultViewModel.created_by_user = message.conversations.created_by_user;
                conversationSearchResultViewModel.id = message.conversationId;
                conversationSearchResultViewModel.type = message.conversations.type;
                conversationSearchResultViewModel.message = message;

                conversationSearchResultViewModels.Add(conversationSearchResultViewModel);
            }


            conversationChatSearchResult = myConversations.Where(x => x.subject.ToLower().Contains(term.ToLower()) && x.type == "chat").ToList();

            conversationDirectSearchResult = myConversations.Where(x => x.members.Any(x => x.user!.username.ToLower().Contains(term.ToLower()) && x.user.id != userId) && x.type == "direct").ToList();


            return (conversationSearchResultViewModels, conversationChatSearchResult, conversationDirectSearchResult);
        }

        public async Task<(IEnumerable<ConversationMessageViewModel>, int, int)> GetSearchMessage(string userId, string conversationId, string messageId)
        {
            List<ConversationMessageViewModel> result = new List<ConversationMessageViewModel>();
            int pageSize = 50;

            var members = Task.Run(async () => await _conversationMembersCollection.Find(x => x.ConversationId == conversationId).ToListAsync()).Result;


            var query = (from m in _conversationMessagesCollection.AsQueryable()
                         join r in _conversationMessageReadReceiptCollection.AsQueryable()
                         on m.Id equals r.messageId into readReceipts
                         where m.ConversationId == conversationId && m.status == 1
                         select new ConversationMessageViewModel
                         {
                             conversationId = m.ConversationId,
                             created_at = m.created_at,
                             created_by = m.created_by,
                             id = m.Id,
                             text = m.Text,
                             readReceipt = readReceipts.Select(x => new ConversationMessageReadReceiptViewModel
                             {
                                 created_at = x.created_at,
                                 isRead = x.isRead,
                                 userId = x.userId,
                             }).ToList(),
                         }).AsQueryable();


            var totalMessages = query.Count();

            var lastMessage = query.OrderByDescending(x => x.created_at.Date).
                          ThenByDescending(c => c.created_at.TimeOfDay).FirstOrDefault();

            var messageToBeSearched = await GetConversationMessage(messageId);

            if (lastMessage.id == messageId)
            {
                int page = 1;

                result = query.OrderByDescending(x => x.created_at.Date).
                              ThenByDescending(c => c.created_at.TimeOfDay).Skip(((page - 1) * pageSize)).Take(pageSize).ToList();
            }
            else
            {
                int recordsCountInBetweenTwoMessages = query.Where(x => x.created_at <= lastMessage.created_at && x.created_at >= messageToBeSearched.created_at).Count();

                int page = 1;

                int size = recordsCountInBetweenTwoMessages / 50;

                if ((recordsCountInBetweenTwoMessages % 50) > 0)
                {
                    pageSize = (size * 50) + 50;
                }
                else
                {
                    pageSize = (size * 50);
                }

                result = query.OrderByDescending(x => x.created_at.Date).
                              ThenByDescending(c => c.created_at.TimeOfDay).Skip(((page - 1) * pageSize)).Take(pageSize).ToList();
            }


            foreach (ConversationMessageViewModel item in result)
            {
                item.conversations = GetConversation(item.conversationId);

                item.conversations.unreadCount = GetConversationUnreadCount(item.created_by, item.id!);

                item.conversations.unread = item.conversations.unreadCount > 0 ? true : false;

                item.readReceipt.ForEach(p => p.member = _usersService.GetDetailedV4(p.userId));

                item.files = (from cf in _conversationFilesCollection.AsQueryable()
                              where cf.MessageId == item.id
                              select new ConversationFileViewModel
                              {
                                  id = cf.Id,
                                  link = cf.Link
                              }).ToList();

                item.created_by_user = _usersService.GetDetailedV4(item.created_by);

                item.editReceipt = (from er in _conversationMessageEditedReceiptCollection.AsQueryable()
                                    where er.messageId == item.id
                                    select new ConversationMessageEditedReceiptViewModel
                                    {
                                        created_at = er.created_at,
                                        userId = er.userId,
                                    }).OrderByDescending(x => x.created_at.Date).
                                ThenByDescending(c => c.created_at.TimeOfDay).FirstOrDefault();

                if (item.editReceipt is not null)
                {
                    item.editReceipt.user = _usersService.GetDetailedV4(item.editReceipt.userId);
                }
            }

            result.Reverse();

            return (result, totalMessages, pageSize);
        }

        public async Task<ConversationMessageViewModel?> EditConversationMessage(string userId, string conversationId, string messageId, string text, List<string>? files)
        {
            var conversationMessage = await _conversationMessagesCollection.Find(x => x.Id == messageId).FirstOrDefaultAsync();

            conversationMessage.Text = text;
            await _conversationMessagesCollection.ReplaceOneAsync(x => x.Id == messageId, conversationMessage);

            if (_conversationFilesCollection.AsQueryable().Any(x => x.MessageId == messageId))
            {
                await _conversationFilesCollection.DeleteManyAsync(x => x.MessageId == messageId);
            }

            if (files is not null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    ConversationFiles conversationFiles = new ConversationFiles();
                    conversationFiles.ConversationId = conversationId;
                    conversationFiles.MessageId = messageId;
                    conversationFiles.Link = file;

                    await _conversationFilesCollection.InsertOneAsync(conversationFiles);
                }
            }

            var conversationMessageEditedReceipt = new ConversationMessageEditedReceipt();
            conversationMessageEditedReceipt.conversationId = conversationId;
            conversationMessageEditedReceipt.created_at = DateTime.UtcNow;
            conversationMessageEditedReceipt.messageId = messageId;
            conversationMessageEditedReceipt.userId = userId;

            await _conversationMessageEditedReceiptCollection.InsertOneAsync(conversationMessageEditedReceipt);

            var updatedConversationMessage = GetMessage(messageId);

            return updatedConversationMessage;
        }

        public async Task RemoveConversationMessage(string messageId)
        {
            var conversationMessage = await _conversationMessagesCollection.Find(x => x.Id == messageId).FirstOrDefaultAsync();

            conversationMessage.status = 0;

            await _conversationMessagesCollection.ReplaceOneAsync(x => x.Id == messageId, conversationMessage);
        }

    }
}
