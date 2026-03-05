
using SparkService.Services;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SparkApp.Hubs
{
    [Authorize]
    public class EmailHub : Hub
    {
        private readonly static ConnectionMapping<string> _connections = new ConnectionMapping<string>();
        private readonly EmailMessageService _emailMessageService;
        public EmailHub(EmailMessageService emailMessageService)
        {
            _emailMessageService = emailMessageService;
        }

        public override Task OnConnectedAsync()
        {
            var claimsIdentity = Context.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x=>x.Type == "id")?.Value;

            _connections.Add(userId, Context.ConnectionId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception ex)
        {
            var claimsIdentity = Context.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x=>x.Type == "id")?.Value;

            _connections.Remove(userId, Context.ConnectionId);

            return base.OnDisconnectedAsync(ex);
        }

        public async Task SendEmailNotification(string userId, object email)
        {
            foreach (var connectionId in _connections.GetConnections(userId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveEmail", (EmailMessageViewModel)email);
            }
        }


    }
}
