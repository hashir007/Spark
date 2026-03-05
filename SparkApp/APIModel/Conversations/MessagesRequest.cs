namespace SparkApp.APIModel.Conversations
{
    public class MessagesRequest
    {
        public int? pageSize { get; set; }
        public int? page { get; set; }
        public string conversationId { get; set; } = null!;
    }
}
