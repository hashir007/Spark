namespace SparkApp.APIModel.User
{
    public class UserTraitsRequest
    {      
        public List<TraitRequest> traits { get; set; }

        public UserTraitsRequest()
        {
            traits = new List<TraitRequest>();
        }
    }
    public class TraitRequest
    {
        public string trait_id { get; set; } = null!;
        public int trait_value { get; set; }
    }

}
