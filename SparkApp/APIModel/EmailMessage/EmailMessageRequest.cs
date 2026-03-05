namespace SparkApp.APIModel.EmailMessage
{
    public class EmailMessageRequest
    {
        public string folderId { get; set; } = null!;

        public string search { get; set; } = null!;
    }
}
