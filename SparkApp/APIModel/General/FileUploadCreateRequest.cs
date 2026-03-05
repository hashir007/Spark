using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.General
{
    public class FileUploadCreateRequest
    {
        [FromForm]
        public IFormFile File { get; set; } = null!;

        [FromForm]
        public string type { get; set; } = null!;
    }
}
