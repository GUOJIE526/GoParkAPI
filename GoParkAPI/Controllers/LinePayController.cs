using GoParkAPI.DTO;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinePayController : ControllerBase
    {
        private readonly LinePayService _linePayService;
        public LinePayController(LinePayService linePayService)
        {
            _linePayService = linePayService;
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
        public async Task<PaymentResponseDto> CreatePayment(PaymentRequestDto dto)
        {
            return await _linePayService.SendPaymentRequest(dto);
        }

        [HttpPost("Confirm")]
        public async Task<PaymentConfirmResponseDto> ConfirmPayment([FromQuery] string transactionId, [FromQuery] string orderId, PaymentConfirmDto dto)
        {
            //return await _linePayService.ConfirmPayment(transactionId, orderId, dto);
            //var paymentModel = _linePayService.MapDtoToModel(dto);
            //await _linePayService.SaveMonthlyRental(paymentModel); // 將資料儲存進資料庫
            return await _linePayService.ConfirmPayment(transactionId, orderId, dto);
        }

        [HttpGet("Cancel")]
        public async void CancelTransaction([FromQuery] string transactionId)
        {
            _linePayService.TransactionCancel(transactionId);
        }   
    }
}
