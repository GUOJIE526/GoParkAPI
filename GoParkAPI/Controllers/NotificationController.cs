using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly PushNotificationService _pushNotificationService;
        private readonly EasyParkContext _context;
        public NotificationController(PushNotificationService pushNotificationService, EasyParkContext easyPark)
        {
            _pushNotificationService = pushNotificationService;
            _context = easyPark;
        }

        [HttpPost("subscribe")]
        public IActionResult Subscribe([FromBody] PushSubscription subscription)
        {
            _pushNotificationService.AddSubscription(subscription);
            return Ok();
        }

        //TEST
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationRequestDTO requestDTO)
        {
            await _pushNotificationService.SendNotificationAsync(requestDTO.Title, requestDTO.Message);
            return Ok(new { title = requestDTO.Title, body = requestDTO.Message, MessageResult = "通知發送成功" });
        }
        [HttpGet("CheckAndSendOverdueReminder")]
        public async Task<IActionResult> CheckAndSendOverdueReminder()
        {
            var now = DateTime.Now;
            var minutesLater = now.AddMinutes(30);

            //檢查未發送通知的預約提醒
            var reservation = await _context.Reservation.Where(r => !r.IsFinish && !r.NotificationStatus && r.StartTime <= minutesLater && r.StartTime > now).ToListAsync();
            

            if (reservation.Count != 0)
            {
                //發送通知和更新紀錄
                foreach (var res in reservation)
                {
                    string title = "預約提醒";
                    string message = "您的預約將在30分鐘後超時,請在安全前提下盡快入場,逾時車位不保留.";
                    await _pushNotificationService.SendNotificationAsync(title, message);
                    res.NotificationStatus = true;
                    return Ok(new {title, message});
                }
                //批量更新
                await _context.SaveChangesAsync();
            }
            return BadRequest();
        }
    }
}
