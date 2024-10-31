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

        public async Task CheckAndSendOverdueReminder(int userId)
        {
            var now = DateTime.Now;
            var minutesLater = now.AddMinutes(30);

            // 獲取該使用者的所有車輛
            var userCars = await _context.Car.Where(x => x.UserId == userId).Select(c => c.CarId).ToListAsync();
            //檢查未發送通知的預約提醒
            var reservation = await _context.Reservation.Where(r => userCars.Contains(r.CarId) && !r.IsFinish && !r.NotificationStatus && r.PaymentStatus && r.StartTime <= minutesLater && r.StartTime > now).ToListAsync();

            if (!reservation.Any())
            {
                // 如果該用戶所有預約都已完成，則停止排程
                RecurringJob.RemoveIfExists($"OverdueReminder_{userId}");
                return;
            }

            if (reservation.Any())
            {
                //發送通知和更新紀錄
                foreach (var res in reservation)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", "預約提醒", "您的預約將在30分鐘後超時, 請在安全前提下盡快入場, 逾時車位不保留.");
                    res.NotificationStatus = true;
                }

                //批量更新
                await _context.SaveChangesAsync();
            }
        }

        public async Task CheckAlreadyOverdueRemider(int userId)
        {
            var now = DateTime.Now;
            var userCars = await _context.Car.Where(x => x.UserId == userId).Select(c => c.CarId).ToListAsync();
            var res = await _context.Reservation.Where(r => userCars.Contains(r.CarId) && r.ValidUntil <= now && r.NotificationStatus && r.PaymentStatus && !r.IsFinish).ToListAsync();

            if (!res.Any())
            {
                // 如果該用戶所有預約都已完成，則停止排程
                RecurringJob.RemoveIfExists($"AlreadyOverdue_{userId}");
                return;
            }

            if (res.Any())
            {
                var cust = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == userId);
                foreach(var re in res)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", "預約超時", "您的預約已超時!");
                    re.IsFinish = true;
                    re.IsOverdue = true;
                }
                cust.BlackCount += 1;
                if(cust.BlackCount >= 3)
                {
                    cust.IsBlack = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
