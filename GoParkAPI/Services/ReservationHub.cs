using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GoParkAPI.Services
{
    public class ReservationHub : Hub
    {
        // 發送通知給特定用戶
        public async Task SendReminder(string userId, string title, string message)
        {
            // 向特定用戶發送通知
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }
    }
}
