using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.EmailMessage
{
    public class EmailMessageMoveCreateRequest
    {
        [FromRoute]
        [BindRequired]
        public string userId { get; set; } = null!;

        [FromBody]
        [BindRequired]
        public EmailMessageMove EmailMessageMove { get; set; } = null!;

    }
    public class EmailMessageMove
    {
        public string email_id { get; set; } = null!;
        public string from_folder_id { get; set; } = null!;
        public string to_folder_id { get; set; } = null!;

    }
}
