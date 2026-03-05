namespace SparkService.ViewModels
{
    public class EmailMessageSendersViewModel
    {
        public string userId { get; set; } = null!;

        public MemberViewModel? user { get; set; }
    }
}
