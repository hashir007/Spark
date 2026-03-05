using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.EmailMessage
{
    public class EmailMessagePurge
    {
        [BindRequired]
        public string? Id { get; set; }

        [BindRequired]
        public string folder_id { get; set; } = null!;
    }
}
