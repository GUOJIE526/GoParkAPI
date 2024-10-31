using GoParkAPI.Models;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace GoParkAPI.Services
{
    public class ReservationNotificationService : IJob
    {
        private readonly ILogger<ReservationNotificationService> _logger;
        private readonly EasyParkContext _context;
        private readonly ReservationHub _reservationHub;
        private readonly ISchedulerFactory _schedulerFactory;

        public ReservationNotificationService(
            ILogger<ReservationNotificationService> logger,
            EasyParkContext easyPark,
            ReservationHub reservationHub,
            ISchedulerFactory schedulerFactory)
        {
            _logger = logger;
            _context = easyPark;
            _reservationHub = reservationHub;
            _schedulerFactory = schedulerFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Checking for overdue and upcoming reservations.");

            var now = DateTime.Now;
            var reminderTime = now.AddMinutes(30); // 30 分鐘前提醒

            // 查詢所有未完成的預約，並依據時間來篩選
            var reservations = _context.Reservation
                .Where(r => !r.IsFinish && !r.NotificationStatus && r.PaymentStatus && r.StartTime <= reminderTime && r.StartTime > now)
                .ToList();

            foreach (var reservation in reservations)
            {
                // 根據 CarId 查詢對應的 UserId
                var car = _context.Car.FirstOrDefault(c => c.CarId == reservation.CarId);
                if (car == null)
                {
                    _logger.LogWarning($"No car found for reservation with CarId: {reservation.CarId}");
                    continue;
                }

                var userId = car.UserId;

                // 發送提醒通知
                await _reservationHub.SendReminder(userId, "您的預約即將超時, 請在安全前提下盡快入場！");

                // 更新通知狀態
                reservation.NotificationStatus = true;
            }

            // 查詢所有已超時但尚未完成的預約
            var overdueReservations = _context.Reservation
                .Where(r => !r.IsFinish && r.ValidUntil <= now)
                .ToList();

            foreach (var reservation in overdueReservations)
            {
                var car = _context.Car.FirstOrDefault(c => c.CarId == reservation.CarId);
                if (car == null)
                {
                    _logger.LogWarning($"No car found for reservation with CarId: {reservation.CarId}");
                    continue;
                }

                var userId = car.UserId;

                // 發送超時通知
                await _reservationHub.SendReminder(userId, "您的預約已超時！");

                // 標記預約已通知
                reservation.NotificationStatus = true;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Reservation check completed.");
        }
        // 排程設定方法
        public async Task ScheduleReservationNotificationJob(int userId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey($"ReservationNotificationJob_{userId}");

            // 檢查是否已經有同一用戶的排程
            if (await scheduler.CheckExists(jobKey))
            {
                _logger.LogInformation($"用戶 {userId} 的通知排程已存在，不需要重新設置。");
                return;
            }

            // 設置定時任務
            var job = JobBuilder.Create<ReservationNotificationService>()
                .WithIdentity(jobKey)
                .UsingJobData("UserId", userId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"ReservationNotificationTrigger_{userId}")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(5)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            _logger.LogInformation($"已為用戶 {userId} 設置通知排程。");
        }

        // 停止排程方法
        public async Task StopReservationNotificationJob(int userId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey($"ReservationNotificationJob_{userId}");

            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.DeleteJob(jobKey);
                _logger.LogInformation($"已刪除用戶 {userId} 的通知排程。");
            }
        }
    }
}
