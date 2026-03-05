using Microsoft.AspNetCore.Mvc;

namespace SparkApp.APIModel.Friendships
{
    public class FriendAddRequest
    {
        [FromRoute]
        public string userId {  get; set; } = null!;

        [FromBody]
        public NewFriend friend { get; set; } = null!;

    }

    public class NewFriend
    {
        public string friend_id { get; set; } = null!;
    }
}
