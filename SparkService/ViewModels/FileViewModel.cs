namespace SparkService.ViewModels
{
    public class FileViewModel
    {
        public string? id { get; set; }      

        public string? type { get; set; }

        public string? name { get; set; }
        public string original { get; set; } = null!;
        public string d480x320 { get; set; } = null!;
        public string d300x300 { get; set; } = null!;
        public string d100x100 { get; set; } = null!;
        public string d32x32 { get; set; } = null!;
        public string d16x16 { get; set; } = null!;

        public double size { get; set; }

        public string orignalName { get; set; } = null!;
    }
}
