namespace SparkApp.APIModel.User
{
    public class UserInterestsRequest
    {
        public List<InterestsRequest> interests { get; set; }

        public UserInterestsRequest()
        {
            interests = new List<InterestsRequest>();
        }
    }

    public class InterestsRequest
    {
        public string interest_description { get; set; } = null!;
        public string category_id { get; set; } = null!;
        public int popularity { get; set; }
    }
}
