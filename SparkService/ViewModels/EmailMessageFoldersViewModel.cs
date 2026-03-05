namespace SparkService.ViewModels
{
    public class EmailMessageFoldersViewModel
    {
        public string name { get; set; } = null!;
        public string? id { get; set; }
        public int? unreadcount { get; set; } = null!;
    }
}
