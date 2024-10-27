using GoParkAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
        
        //注入VapidConfig
        public PushNotificationService(VapidConfig vapidConfig, ILogger<PushNotificationService> logger, EasyParkContext easyPark)
        {
            _vapidConfig = vapidConfig;
            _logger = logger;
            _context = easyPark;
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
                var payload = JsonSerializer.Serialize(new {title, body = message});
                try
                {
                    //VAPID發送
                    await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
                }
                catch(WebPushException ex)
                {
                    //失敗
                    _logger.LogError(ex, "Failed to send notification to {Endpoint}", subscription.Endpoint);
                    _subscriptions.TryRemove(subscription.Endpoint, out _);
                }
            }
        }

        public async Task CheckAndSendOverdueReminder()
        {
            var now = DateTime.Now;
            var minutesLater = now.AddMinutes(30);

            //檢查未發送通知的預約提醒
            var reservation = await _context.Reservation.Where(r => !r.IsFinish && !r.NotificationStatus && r.StartTime <= minutesLater && r.StartTime > now).ToListAsync();

            if(reservation.Any())
            {
                //發送通知和更新紀錄
                foreach (var res in reservation)
                {
                    await SendNotificationAsync("預約提醒", "您的預約將在30分鐘後超時, 請在安全前提下盡快入場, 逾時車位不保留.");
                    res.NotificationStatus = true;
                }

                //批量更新
                await _context.SaveChangesAsync();
            }
        }
    }
}
