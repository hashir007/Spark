using SparkApp.Controllers;
using SparkService.Models;
using SparkService.Services;

namespace SparkApp.Services
{
    public class UserNotificationService
    {
        private readonly ILogger<UserNotificationService> _logger;
        private readonly NotificationsService _notificationsService;
        public UserNotificationService(ILogger<UserNotificationService> logger, NotificationsService notificationsService) => (_logger, _notificationsService) = (logger, notificationsService);

        public async Task SendNotificationForEvent(string data, NotificationType type, string userId)
        {
            try
            {
                await _notificationsService.AddAsync(new SparkService.Models.Notifications()
                {
                    created_at = DateTime.UtcNow,
                    data = data,
                    is_read = false,
                    type = type.ToString(),
                    user_id = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkApp.Services.UserNotificationService.SendNotificationForEvent Error = {ex.Message}");
            }
        }

    }
}
