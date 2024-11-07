using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ECPayController : ControllerBase
    {
        private const string HashKey = "pwFHCqoQZGmho4w6";
        private const string HashIV = "EkRm7iFT261dpevs";
        private readonly EasyParkContext _context;
        private readonly ECService _ecService;
        private readonly MailService _sentmail;

        public ECPayController(EasyParkContext context, ECService ecService, MailService sentmail)
        {
            _context = context;
            _ecService = ecService;
            _sentmail = sentmail;
        }

        // 1. 生成 ECPay 表單
        [HttpPost("ECPayForm")]
        public async Task<IActionResult> CreateECPayForm([FromBody] ECpayDTO dto)
        {
            var lot = await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == dto.LotId);
            if (lot == null) return BadRequest("無效的停車場");

            var merchantTradeNo = "MyGo" + DateTime.Now.ToString("yyyyMMddHHmmss");
            dto.OrderId = merchantTradeNo;

            // 構建支付參數
            var paymentParameters = new Dictionary<string, string>
            {
                { "MerchantID", "3002607" },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", $"{dto.TotalAmount}" },
                { "TradeDesc", dto.ItemName },
                { "ItemName", $"{lot.LotName}({dto.ItemName}) - {dto.PlanName}" },
                { "ReturnURL", "https://19fd-182-235-134-21.ngrok-free.app/api/ECPay/Callback" },
                { "ClientBackURL", $"{dto.ClientBackURL}?MerchantTradeNo={merchantTradeNo}" },
                { "ChoosePayment", "ALL" }
            };

            // 生成檢核碼並添加到參數
            string checkMacValue = GenerateCheckMacValue(paymentParameters);
            paymentParameters.Add("CheckMacValue", checkMacValue);

            // 保存租賃記錄
            MonthlyRental rentalRecord = _ecService.MapDtoToModel(dto);
            await _context.MonthlyRental.AddAsync(rentalRecord);
            await _context.SaveChangesAsync();

            return Ok(paymentParameters);
        }

        // 2. 生成檢核碼
        private string GenerateCheckMacValue(Dictionary<string, string> parameters)
        {
            var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                                         .Select(p => $"{p.Key}={p.Value}")
                                         .ToList();

            string paramString = $"HashKey={HashKey}&" + string.Join("&", sortedParams) + $"&HashIV={HashIV}";
            string urlEncodedString = HttpUtility.UrlEncode(paramString).ToLower();

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(urlEncodedString));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }

        [HttpPost("Callback")]
        public async Task<IActionResult> Callback([FromForm] ECpayCallbackDTO callbackData)
        {
            if (callbackData == null || string.IsNullOrEmpty(callbackData.MerchantTradeNo))
                return BadRequest("無效的回傳資料");

   
            
            // 查詢 Customer 資料
            var customer = await _context.MonthlyRental
                .Where(m => m.TransactionId == callbackData.MerchantTradeNo)
                .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
                .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }



            // 根據回傳的 RtnCode 更新交易狀態
            if (callbackData.RtnCode == "1") // 1 代表交易成功
            {
                // 更新支付狀態
                var (success, message) = await _ecService.UpdatePaymentStatusAsync(callbackData.MerchantTradeNo);

                if (!success)
                {
                    return NotFound(new { success = false, message });
                }


                // 準備佔位符的值
                var placeholders = new Dictionary<string, string>
                {
                    { "username", customer.Username},
                    { "message", "您的月租已確認，感謝您使用 MyGoParking！" }
                };
    
                // 指定模板路徑
                string templatePath = "Templates/EmailTemplate.html";

                // 讀取模板並替換佔位符
                string emailBody = await _sentmail.LoadEmailTemplateAsync(templatePath, placeholders);

                try
                {
                    // 發送郵件
                    await _sentmail.SendEmailAsync(customer.Email, "MyGoParking 通知", emailBody);
                    Console.WriteLine($"成功發送郵件至 {customer.Email}");
                }
                catch (Exception ex)
                {
                    // 捕捉並記錄郵件發送錯誤
                    Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
                }

                // 4. 返回成功回應
                return Ok(new { success = true, message = "支付狀態更新成功並已發送通知。" });
            }

            await _context.SaveChangesAsync();

            return Ok("回傳資料處理完成");
        }


        //--------------------------------------------------------------------------------------------------

        [HttpGet("CheckPaymentStatus")]
        public async Task<IActionResult> CheckPaymentStatus([FromQuery] string MerchantTradeNo)
        {
            if (string.IsNullOrEmpty(MerchantTradeNo))
                return BadRequest("無效的交易編號");

            // 查找交易記錄
            var rentalRecord = await _context.MonthlyRental.FirstOrDefaultAsync(r => r.TransactionId == MerchantTradeNo);
            if (rentalRecord == null)
                return NotFound(new { status = "交易不存在" });

            // 返回交易狀態
            return Ok(new { status = rentalRecord.PaymentStatus ? "已支付" : "未支付" });
        }

        //--------------------------------------------------------------------------------------------------


    }

}
