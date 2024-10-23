using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Security.Policy;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinePayController : ControllerBase
    {
        private readonly LinePayService _linePayService;
        private readonly EasyParkContext _context;
        private readonly MailService _sentmail;
        public LinePayController(LinePayService linePayService, EasyParkContext context, MailService sentmail)
        {
            _linePayService = linePayService;
            _context = context;
            _sentmail = sentmail;
        }



        // 新增 ValidatePayment 方法，使用 Service 進行驗證
        [HttpPost("Validate")]
        public IActionResult ValidatePayment([FromBody] PaymentValidationDto request)
        {
            try
            {
                // 驗證資料是否有效
                if (string.IsNullOrEmpty(request.PlanId) || request.Amount <= 0)
                {
                    return BadRequest(new { message = "無效的方案 ID 或金額。" });
                }

                // 呼叫服務層進行驗證
                bool isValid = _linePayService.ValidatePayment(request.PlanId, request.Amount);

                if (!isValid)
                {
                    return BadRequest(new { message = "方案或金額驗證失敗。" });
                }

                return Ok(new { isValid = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"伺服器錯誤: {ex.Message}");
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }


        [HttpPost("Create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequestDto dto)
        {
            try
            {
                // 使用 LinePay 服務發送支付請求
                var paymentResponse = await _linePayService.SendPaymentRequest(dto);

                // 將 DTO 映射為 MonthlyRental 模型
                MonthlyRental rentalRecord = _linePayService.MapDtoToModel(dto);

                // 將租賃記錄新增到資料庫
                await _context.MonthlyRental.AddAsync(rentalRecord);

                // 保存變更
                await _context.SaveChangesAsync();

                // 回傳支付結果
                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                // 記錄錯誤（建議使用 ILogger）
                Console.WriteLine($"發生錯誤：{ex.Message}");

                // 回傳錯誤回應
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "處理支付時發生錯誤。" });
            }
        }



        [HttpPost("Confirm")]
        public async Task<PaymentConfirmResponseDto> ConfirmPayment([FromQuery] string transactionId, [FromQuery] string orderId, PaymentConfirmDto dto)
        {
            //return await _linePayService.ConfirmPayment(transactionId, orderId, dto);
            //var paymentModel = _linePayService.MapDtoToModel(dto);
            //await _linePayService.SaveMonthlyRental(paymentModel); // 將資料儲存進資料庫
            return await _linePayService.ConfirmPayment(transactionId, orderId, dto);
        }


        //[HttpPost("CreateRental")]

     




        [HttpGet("Cancel")]
        public async void CancelTransaction([FromQuery] string transactionId)
        {
            _linePayService.TransactionCancel(transactionId);
        }
    }
}
