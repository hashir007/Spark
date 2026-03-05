using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SparkApp.APIModel.User
{
    public class AuthenticationExternalRequest
    {

        public string source { get; set; } = null!;

        public string access_token { get; set; } = null!;


        public string timezone { get; set; } = null!;


        public string language { get; set; } = null!;

    }
}
