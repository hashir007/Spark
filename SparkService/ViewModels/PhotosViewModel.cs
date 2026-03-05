using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.ViewModels
{
    public class PhotosViewModel
    {
        public string? Id { get; set; }
        public string fileId { get; set; } = null!;
        public string? passCode { get; set; }
        public FileViewModel file { get; set; } = null!;
        public string? userId { get; set; } = null!;
        public UserViewModelV3 user { get; set; } = null!;
        public bool is_private { get; set; }
        public bool is_adult { get; set; }
        public bool is_featured { get; set; }
        public bool is_members_only { get; set; }

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
