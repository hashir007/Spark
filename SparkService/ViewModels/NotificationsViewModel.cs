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
    public class NotificationsViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_user_id")]
        public string user_id { get; set; } = null!;

        public string data { get; set; } = null!;

        public bool is_read { get; set; }

        public string type { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }

        public NotificationsViewModel ToNotificationsViewModel(Notifications model)
        {
            return new NotificationsViewModel()
            {
                id = model.Id,
                user_id = model.user_id,
                data = model.data,
                is_read = model.is_read,
                created_at = model.created_at,
            };
        }
    }
}
