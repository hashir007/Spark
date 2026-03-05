using SparkService.Models;
using System.Net.Mail;

namespace SparkApp.Models
{
    public class GooglePeopleModel
    {
        public string resourceName { get; set; }
        public string etag { get; set; }
        public List<Name> names { get; set; }
        public List<Photo> photos { get; set; }
        public List<Gender> genders { get; set; }
        public List<Birthday> birthdays { get; set; }
        public List<EmailAddress> emailAddresses { get; set; }
    }
    public class Source
    {
        public string type { get; set; }
        public string id { get; set; }
    }

    public class Birthday
    {
        public Metadata metadata { get; set; }
        public Date date { get; set; }
    }

    public class Date
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
    }

    public class EmailAddress
    {
        public Metadata metadata { get; set; }
        public string value { get; set; }
    }

    public class Gender
    {
        public Metadata metadata { get; set; }
        public string value { get; set; }
        public string formattedValue { get; set; }
    }

    public class Metadata
    {
        public bool primary { get; set; }
        public Source source { get; set; }
        public bool sourcePrimary { get; set; }
        public bool verified { get; set; }
    }

    public class Name
    {
        public Metadata metadata { get; set; }
        public string displayName { get; set; }
        public string familyName { get; set; }
        public string givenName { get; set; }
        public string displayNameLastFirst { get; set; }
        public string unstructuredName { get; set; }
    }

    public class Photo
    {
        public Metadata metadata { get; set; }
        public string url { get; set; }
    }
}
