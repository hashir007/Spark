using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SparkService.Models
{
    public class UserRoles
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }


        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_userid")]
        public string? UserId { get; set; }

       
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_roleid")]
        public string? RoleId { get; set; }
    }
}
