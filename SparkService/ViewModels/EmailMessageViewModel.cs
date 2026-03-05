using MongoDB.Bson.Serialization.Attributes;

namespace SparkService.ViewModels
{
    public class EmailMessageViewModel
    {
        public string? id { get; set; }
        public string content { get; set; } = null!;
        public string subject { get; set; } = null!;
        public string? reply_to_message_id { get; set; }

        public bool is_read { get; set; }

        public bool is_starred { get; set; }

        public List<EmailMessageViewModel> reply_to_message { get; set; }


        public string? created_by { get; set; }
        public MemberViewModel? created_by_user { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
        public string status { get; set; } = null!;
        public List<EmailMessageAttachmentsViewModel> attachments { get; set; }

        public List<EmailMessageRecipientsViewModel> recipients { get; set; }

        public List<EmailMessageSendersViewModel> senders { get; set; }
        public EmailMessageViewModel()
        {
            this.attachments = new List<EmailMessageAttachmentsViewModel>();
            this.recipients = new List<EmailMessageRecipientsViewModel>();
            this.senders = new List<EmailMessageSendersViewModel>();
        }

    }
}
