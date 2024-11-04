﻿using GoParkAPI.Models;
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

        public async Task<bool> CheckAndSendOverdueReminder(int resId)
        {
            var now = DateTime.Now;
            var minutesLater = now.AddMinutes(30);

            // 根據條件查找第一個符合條件的 Reservation 記錄
            var res = await _context.Reservation.FirstOrDefaultAsync(r => r.ResId == resId);
            var user = await _context.Reservation.Where(r => r.ResId == resId).Select(r => r.Car.UserId).FirstOrDefaultAsync();

            if (res.StartTime <= minutesLater && res.StartTime > now && res.PaymentStatus && !res.NotificationStatus && !res.IsFinish)
            {
                res.NotificationStatus = true;
                await _context.SaveChangesAsync();
                await _hubContext.Clients.User(user.ToString()).SendAsync("ReceiveNotification", "預約提醒", "您的預約將在30分鐘後超時，請在安全前提下盡快入場，逾時車位不保留。");
                //啟動Hangfire CheckAlreadyOverdueRemider
                RecurringJob.AddOrUpdate($"AlreadyOverdueReminder_{resId}", () => CheckAlreadyOverdueRemider(resId), "*/2 * * * *");
                return true;
            }
            // 如果沒有符合條件的預約，或者預約已完成或已通知，則刪除排程
            if (res.IsFinish || res.NotificationStatus)
            {
                RecurringJob.RemoveIfExists($"OverdueReminder_{resId}");
                return false;
            }
            return false;
        }
        public async Task<bool> CheckAlreadyOverdueRemider(int resId)
        {
            var now = DateTime.Now;
            var reservation = await _context.Reservation.FirstOrDefaultAsync(r => r.ResId == resId);
            var car = await _context.Car.FirstOrDefaultAsync(c => c.CarId == reservation.CarId);
            var user = await _context.Customer.FirstOrDefaultAsync(u => u.UserId == car.UserId);
            var userId = user.UserId;
            if (reservation.ValidUntil < now && !reservation.IsFinish && reservation.NotificationStatus)
            {
                reservation.IsFinish = true;
                reservation.IsOverdue = true;
                user.BlackCount += 1;
                if (user.BlackCount >= 3)
                {
                    user.IsBlack = true;
                }
                await _context.SaveChangesAsync();
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", "預約超時提醒", "你的預約已超時!!");
                return true;
            }
            if (reservation.IsFinish)
            {
                RecurringJob.RemoveIfExists($"AlreadyOverdueReminder_{resId}");
                return false;
            }

            return false;
        }
    }
}
