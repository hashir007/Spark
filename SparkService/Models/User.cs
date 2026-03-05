using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel;
using Newtonsoft.Json;

namespace SparkService.Models
{
    public enum Gender
    {
        Male,
        Female,
        [Description("Non-binary")]
        NonBinary,
        Genderqueer,
        Genderfluid,
        Agender,
        Bigender,
        Androgynous,
        [Description("Two-Spirit")]
        TwoSpirit,
        Transgender,
        [Description("Gender nonconforming")]
        Gendernonconforming,
        Demigender,
        Neutrois,
        Pangender,
        [Description("Third Gender")]
        ThirdGender,
        Polygender,
        [Description("Gender questioning")]
        Genderquestioning,
        Other

    }
    public enum Iam
    {
        [Description("Sugar Baby")]
        SugarBaby,
        [Description("Sugar Daddy")]
        SugarDaddy,
        [Description("Sugar Momma")]
        SugarMomma,
        [Description("Sugar Baby (Gay)")]
        SugarBabyGay,
        [Description("Sugar Daddy (Gay)")]
        SugarDaddyGay,
        [Description("Sugar Momma (Gay)")]
        SugarMommaGay,
    }
    public enum Seeking
    {
        [Description("Sugar Baby")]
        SugarBaby,
        [Description("Sugar Daddy")]
        SugarDaddy,
        [Description("Sugar Momma")]
        SugarMomma,
        [Description("Sugar Baby (Gay)")]
        SugarBabyGay,
        [Description("Sugar Daddy (Gay)")]
        SugarDaddyGay,
        [Description("Sugar Momma (Gay)")]
        SugarMommaGay,
    }
    public enum EducationLevel
    {
        [Description("High School")]
        HighSchool,
        [Description("Some College")]
        SomeCollege,
        [Description("Associates Degree")]
        AssociatesDegree,
        [Description("Bachelors Degree")]
        BachelorsDegree,
        [Description("Graduate Degree")]
        GraduateDegree,
        [Description("Doctorate")]
        Doctorate
    }
    public enum RelationshipGoals
    {
        [Description("Try to go a few days without needing each other")]
        Try_to_go_a_few_days_without_needing_each_other,
        [Description("Have daily conversations")]
        Have_daily_conversations,
        [Description("Strive to become each other’s best friend")]
        Strive_to_become_each_others_best_friend,
        [Description("Have each other’s back")]
        Have_each_others_back,
        [Description("Support each other’s dreams and goals")]
        Support_each_others_dreams_and_goals

    }
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string username { get; set; } = null!;
        public string password { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime last_password_reset { get; set; }
        public string email_address { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime created_at { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime updated_at { get; set; }
        public string ip_address { get; set; } = null!;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime last_login { get; set; }
        public bool is_email_verified { get; set; }
        public bool is_photo_uploaded { get; set; }
        public bool is_active { get; set; }
        public string timezone { get; set; } = null!;
        public string language { get; set; } = null!;

    }


}
