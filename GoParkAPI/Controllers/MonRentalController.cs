using GoParkAPI.Models;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonRentalController : ControllerBase
    {
        private readonly MonRentalService _monRenService;
        public MonRentalController(MonRentalService monRentalService)
        {
            _monRenService = monRentalService;
        }

        [HttpGet("CheckMonRentalSpace")]
        public async Task<IActionResult> CheckMonRentalSpace(int lotId)
        {
            bool isAvailable = await _monRenService.isMonResntalSpaceAvailableAsync(lotId);
            if (!isAvailable)
            {
                var parkinglots = await _monRenService.GetParkingLotAsync(lotId);
                if(parkinglots == null)
                {
                    return Ok(new { Message = "無效的停車場ID", Success = false });
                }
                if(parkinglots.MonRentalRate <= 0)
                {
                    return Ok(new { Message = "該停車場不支援月租服務", Success = false });
                }
                return Ok(new {Message = "月租車位已滿, 您可以填寫申請表單等待抽籤", Success = false });
            } 
            return Ok(new {Message = "月租車位可用", Success = true });
        }
    }
}
