using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GoParkAPI.Services
{
    public class ReservationHub : Hub
    {
        private static readonly ConcurrentDictionary<int, string> UserConnections = new ConcurrentDictionary<int, string>();
        // 發送通知給特定用戶
        public async Task SendReminder(int userId, string message)
        {
            if (UserConnections.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
            }
        }
    }
}
