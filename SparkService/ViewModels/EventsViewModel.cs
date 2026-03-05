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
    public class EventsViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string name { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime start_date { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_venueId")]
        public string venueId { get; set; } = null!;
        public Venues Venues { get; set; } = null!;

        public string description { get; set; } = null!;
        public string type { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_organizerId")]
        public string organizerId { get; set; } = null!;
        public EventOrganizers Organizers { get; set; } = null!;    

        public int capacity { get; set; }
        public int attendees { get; set; }
        public string status { get; set; } = null!;
        public string created_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        public string modified_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }


        public EventsViewModel ToEventsViewModel(Events model)
        {
            return new EventsViewModel()
            {
                id = model.Id,
                attendees = model.attendees,
                capacity = model.capacity,
                created_at = model.created_at,
                modified_by = model.modified_by,
                status = model.status,
                created_by = model.created_by,
                description = model.description,
                name = model.name,
                organizerId = model.organizerId,
                start_date = model.start_date,
                type = model.type,
                updated_at = model.updated_at,
                venueId = model.venueId,                
            };
        }
    }
}
