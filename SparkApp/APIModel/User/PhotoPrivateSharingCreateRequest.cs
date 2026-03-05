using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.User
{
    public class PhotoPrivateSharingCreateRequest
    {
        [FromRoute]
        [BindRequired]
        public string id { get; set; } = null!;

        [FromRoute]
        [BindRequired]
        public string photoId { get; set; } = null!;


        [FromBody]
        [BindRequired]
        public PhotoPrivateSharingRequest PhotoPrivateSharingRequest { get; set; } = null!;
        
    }

    public class PhotoPrivateSharingRequest
    {
        public List<string> users { get; set; } = null!;      
    }
}
