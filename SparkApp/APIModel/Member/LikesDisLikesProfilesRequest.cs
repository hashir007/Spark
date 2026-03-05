namespace SparkApp.APIModel.Member
{
    public class LikesDisLikesProfilesRequest
    {
        public string user_id { get; set; } = null!;
        public string profile_id { get; set; } = null!;
        public bool isLikes { get; set; }
    }
}
