namespace SparkApp.APIModel.Notifications
{
    public class NotificationMarkReadRequest
    {
        public List<string> notifications { get; set; } = null!;


        public NotificationMarkReadRequest()
        {
            notifications = new List<string>();
        }
    }
}
