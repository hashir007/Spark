namespace SparkService.ViewModels
{
    public class EmailMessageRecipientsViewModel
    {
        public string userId { get; set; } = null!;

        public MemberViewModel? user { get; set; }
    }
}
