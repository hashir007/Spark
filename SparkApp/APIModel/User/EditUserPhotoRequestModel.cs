namespace SparkApp.APIModel.User
{
    public class EditUserPhotoRequestModel
    {
        public string fileId { get; set; } = null!;
        public bool is_private { get; set; }
        public bool is_adult { get; set; }
        public bool is_featured { get; set; }
        public bool is_members_only { get; set; }
        public string? passCode { get; set; } 
        public string? Id { get; set; }
    }
}
