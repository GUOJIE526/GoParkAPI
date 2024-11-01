using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using WebPush;

namespace GoParkAPI.Services
{
    public class ReservationHub : Hub
    {
        private static Dictionary<string, string> UserConnections = new Dictionary<string, string>();
        private readonly PushNotificationService _pushNotificationService;

        public ReservationHub(PushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"];

            if (int.TryParse(userId, out int parsedUserId))
            {
                UserConnections[parsedUserId.ToString()] = Context.ConnectionId;
            }

            await base.OnConnectedAsync();
        }

        public async Task SendOverdueReminderNotification(string userId)
        {
            var notificationSent = await _pushNotificationService.CheckAndSendOverdueReminder();
            if (notificationSent && UserConnections.TryGetValue(userId, out string connectionId))
            {
                await Clients.All.SendAsync(
                    "ReceiveNotification",
                    "預約提醒",
                    "您的預約將在30分鐘後超時，請在安全前提下盡快入場，逾時車位不保留。"
                );
            }
        }

        public async Task SendAlreadyOverdueReminderNotification(string userId)
        {
            var notificationSent = await _pushNotificationService.CheckAlreadyOverdueRemider();
            if (notificationSent && UserConnections.TryGetValue(userId, out string connectionId))
            {
                await Clients.All.SendAsync(
                    "ReceiveNotification",
                    "預約超時提醒",
                    "你的預約已超時!!"
                );
            }
        }
    }
}
