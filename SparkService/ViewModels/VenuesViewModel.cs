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
    public class VenuesViewModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }
        public string name { get; set; } = null!;
        public string street { get; set; } = null!;
        public string street2 { get; set; } = null!;
        public string city { get; set; } = null!;
        public string state { get; set; } = null!;
        public string zip { get; set; } = null!;
        public string country { get; set; } = null!;
        public string manager_name { get; set; } = null!;
        public string manager_phone { get; set; } = null!;
        public string manager_email { get; set; } = null!;
        public string created_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        public string modified_by { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }


        public VenuesViewModel ToVenuesViewModel(Venues model)
        {
            return new VenuesViewModel
            {
                id = model.Id,
                name = model.name,
                street = model.street,
                street2 = model.street2,
                city = model.city,
                state = model.state,                    
                zip = model.zip,                    
                country = model.country,
                manager_name = model.manager_name,
                manager_phone = model.manager_phone,                    
                manager_email = model.manager_email,                    
                created_by = model.created_by,   
                created_at = model.created_at,
                modified_by = model.modified_by,
                updated_at = model.updated_at
            };
        }
    }
}
