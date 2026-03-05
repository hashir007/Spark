namespace SparkApp.Models
{
    public class FacebookModel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public Picture picture { get; set; }
    }
    public class Data
    {
        public string url { get; set; }
    }

    public class Picture
    {
        public Data data { get; set; }
    }
}
