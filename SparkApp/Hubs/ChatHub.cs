using SparkApp.APIModel.Conversations;
using SparkService.Models;
using SparkService.Services;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SparkApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ConversationService _conversationService;

        public ChatHub(ConversationService conversationService)
        {
            _conversationService = conversationService;
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            Console.WriteLine(Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }
        public async Task JoinRoom(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessageToGroupAsync(ConversationMessageViewModel message, string EVENT_NAME)
        {
            await Clients.Group(message.conversationId.ToString()).SendAsync(EVENT_NAME, message);
        }
        public async Task SendObjectToGroupAsync(string conversationId, object data, string EVENT_NAME)
        {
            await Clients.Group(conversationId).SendAsync(EVENT_NAME, data);
        }
    }
}
