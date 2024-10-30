using GoParkAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
    }
}
