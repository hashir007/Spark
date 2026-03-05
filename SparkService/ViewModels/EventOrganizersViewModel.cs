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
    public class EventOrganizersViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string name { get; set; } = null!;
        public string type { get; set; } = null!;
        public string email { get; set; } = null!;
        public string created_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        public string modified_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }

        public EventOrganizersViewModel ToEventOrganizersViewModel(EventOrganizers model)
        {
            return new EventOrganizersViewModel()
            {
                id = model.Id,
                created_at = model.created_at,
                modified_by = model.modified_by,
                type = model.type,
                email = model.email,
                created_by = model.created_by,
                name = model.name,
                updated_at = model.updated_at                  
            };
        }
    }
}
