namespace SparkApp.APIModel.Conversations
{
    public class MessageCreate
    {       
        public string Text { get; set; } = null!;
        public string? reply_to_message_id { get; set; }
        public List<string>? Files { get; set; }
    }
}
