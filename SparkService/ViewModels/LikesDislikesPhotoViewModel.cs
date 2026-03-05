using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SparkService.Models;

namespace SparkService.ViewModels
{
    public class LikesDislikesPhotoViewModel
    {
        public int totalLikesCount {  get; set; }
        public int totalDislikesCount { get; set; }

        public LikesDisLikesPhoto? LikesDisLikesPhoto { get; set; } 
    }
}
