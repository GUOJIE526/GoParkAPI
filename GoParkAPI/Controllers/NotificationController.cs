using GoParkAPI.DTO;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebPush;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly PushNotificationService _pushNotificationService;
        public NotificationController(PushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
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
    }
}
