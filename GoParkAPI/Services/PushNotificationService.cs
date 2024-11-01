using GoParkAPI.Models;
using Hangfire;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;
using WebPush;

namespace GoParkAPI.Services
{
    public class PushNotificationService
    {
        private readonly VapidConfig _vapidConfig;
        private readonly ConcurrentDictionary<string, PushSubscription> _subscriptions = new();
        private readonly ILogger<PushNotificationService> _logger;
        private readonly EasyParkContext _context;
        private readonly IHubContext<ReservationHub> _hubContext;

        //注入VapidConfig
        public PushNotificationService(VapidConfig vapidConfig, ILogger<PushNotificationService> logger, EasyParkContext easyPark, IHubContext<ReservationHub> hubContext)
        {
            _vapidConfig = vapidConfig;
            _logger = logger;
            _context = easyPark;
            _hubContext = hubContext;
        }
        //新增訂閱資訊
        public void AddSubscription(PushSubscription subscription)
        {
            _subscriptions[subscription.Endpoint] = subscription;
        }
        //發送通知給有訂閱的
        public async Task SendNotificationAsync(string title, string message)
        {
            var webPushClient = new WebPushClient();
            var vapidDetails = _vapidConfig.GetVapidDetails();
            foreach (var subscription in _subscriptions.Values)
            {
                //準備推波內容
                var payload = JsonSerializer.Serialize(new { title, body = message });
                try
                {
                    //VAPID發送
                    await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
                }
                catch (WebPushException ex)
                {
                    //失敗
                    _logger.LogError(ex, "Failed to send notification to {Endpoint}", subscription.Endpoint);
                    _subscriptions.TryRemove(subscription.Endpoint, out _);
                }
            }
        }

        public async Task<bool> CheckAndSendOverdueReminder()
        {
            var now = DateTime.Now;
            var minutesLater = now.AddMinutes(30);

            // 根據條件查找第一個符合條件的 Reservation 記錄
            var res = await _context.Reservation
                .FirstOrDefaultAsync(r => !r.IsFinish
                                       && !r.NotificationStatus
                                       && r.StartTime <= minutesLater
                                       && r.StartTime > now
                                       && r.PaymentStatus);

            // 如果沒有符合條件的預約，或者預約已完成或已通知，則刪除排程
            if (res == null)
            {
                // 如果不依賴具體的 resId 可以省略 RemoveIfExists
                RecurringJob.RemoveIfExists($"OverdueReminder");
                return false;
            }
            res.NotificationStatus = true;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "預約提醒", "您的預約將在30分鐘後超時，請在安全前提下盡快入場，逾時車位不保留。");
            return true;
        }

        public async Task<bool> CheckAlreadyOverdueRemider()
        {
            var now = DateTime.Now;
            var reservation = await _context.Reservation.FirstOrDefaultAsync(r => r.ValidUntil < now && !r.IsFinish);
            var car = await _context.Car.FirstOrDefaultAsync(c => c.CarId == reservation.CarId);
            var user = await _context.Customer.FirstOrDefaultAsync(u => u.UserId == car.UserId);
            //var res = await _context.Reservation.FirstOrDefaultAsync(r => r.ResId == resId);
            if (reservation == null)
            {
                RecurringJob.RemoveIfExists($"AlreadyOverdueReminder");
                return false;
            }
            reservation.IsFinish = true;
            reservation.IsOverdue = true;
            user.BlackCount += 1;
            if(user.BlackCount >= 3)
            {
                user.IsBlack = true;
            }
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "預約超時提醒", "你的預約已超時!!");
            return true;
        }
    }
}
