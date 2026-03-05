using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.EmailMessage
{

    public class EmailMessageCreateRequest
    {

        [FromRoute]
        [BindRequired]
        public string userId { get; set; } = null!;

        [FromBody]
        [BindRequired]
        public EmailMessageCreate emailMessageCreate { get; set; } = null!;


    }

    public class EmailMessageCreate
    {
        [Required]
        public string content { get; set; } = null!;
        [Required]
        public string subject { get; set; } = null!;

        public string? reply_to_message_id { get; set; }

        public List<string> attachments { get; set; }
        public List<string> senders { get; set; }
        public List<string> recipients { get; set; }

        public EmailMessageCreate()
        {
            this.attachments = new List<string>();
            this.senders = new List<string>();
            this.recipients = new List<string>();
        }
    }

}