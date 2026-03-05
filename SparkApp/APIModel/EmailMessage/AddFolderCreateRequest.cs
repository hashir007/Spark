using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.EmailMessage
{
    public class AddFolderCreateRequest
    {
        [FromRoute]
        [BindRequired]
        public string userId { get; set; } = null!;

        [FromBody]
        [BindRequired]
        public NewFolder newFolder { get; set; } = null!;

    }

    public class NewFolder
    {
        public string name { get; set; } = null!;
    }
}
