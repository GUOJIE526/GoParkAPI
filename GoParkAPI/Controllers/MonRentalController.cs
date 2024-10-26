using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonRentalController : ControllerBase
    {
        private readonly MonRentalService _monRenService;
        private readonly EasyParkContext _context;
        public MonRentalController(MonRentalService monRentalService, EasyParkContext easyPark)
        {
            _monRenService = monRentalService;
            _context = easyPark;
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
        [HttpPost("newMonApplyList")]
        public async Task<IActionResult> newMonApplyList([FromBody] MonApplyDTO monApplayDTO)
        {
            try
            {
                if(monApplayDTO.UserId == null || monApplayDTO.UserId == 0)
                {
                    return BadRequest(new { Message = "無法取得用戶ID" });
                }

                var car = await _context.Car.FirstOrDefaultAsync(c => c.LicensePlate == monApplayDTO.LicensePlate && c.UserId == monApplayDTO.UserId);
                if (car == null)
                {
                    return BadRequest(new { Message = "無法找到對應的車輛" });
                }
                //寫進MonApplyList
                var newApplay = new MonApplyList
                {
                    CarId = car.CarId,
                    LotId = monApplayDTO.LotId,
                    ApplyDate = DateTime.Now,
                };
                _context.MonApplyList.Add(newApplay);
                await _context.SaveChangesAsync();

                return Ok(new {Message = "申請已成功提交", applyID = newApplay.ApplyId});//debug方便給前端一個id
                }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
