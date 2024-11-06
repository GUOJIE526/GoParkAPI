using GoParkAPI.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public class ECPayController : ControllerBase
{
    private const string HashKey = "pwFHCqoQZGmho4w6";
    private const string HashIV = "EkRm7iFT261dpevs";

    [HttpPost("ECPayForm")]
    public IActionResult GenerateECPayForm([FromBody] ECpayDTO? dto)
    {
        if (dto == null)
        {
            return BadRequest("Invalid input data.");
        }
   
        // 構建支付參數字典，包括檢查碼
        var paymentParameters = new Dictionary<string, string>
        {
            { "MerchantID", "3002607" },
            { "MerchantTradeNo", "MyGo" + DateTime.Now.ToString("yyyyMMddHHmm") },
            { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
            { "PaymentType", "aio" },
            { "TotalAmount", $"{dto.TotalAmount}" },
            { "TradeDesc",  dto.ItemName},
            { "ItemName", dto.ItemName + " - " + dto.PlanName },
            { "ReturnURL", "http://example.com" },
            { "ClientBackURL", dto.ClientBackURL },
            { "ChoosePayment", "ALL" }
        };

        // 生成檢核碼
        string checkMacValue = GenerateCheckMacValue(paymentParameters);
        paymentParameters.Add("CheckMacValue", checkMacValue);
        Console.WriteLine(paymentParameters);
        // 回傳支付參數作為 JSON，不再額外回傳 checkMacValue
        return Ok(paymentParameters);
    }

    private string GenerateCheckMacValue(Dictionary<string, string> parameters)
    {
        // 1. 排序參數
        var sortedParams = parameters
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}")
            .ToList();

        // 2. 加上 HashKey 和 HashIV
        string paramString = $"HashKey={HashKey}&" + string.Join("&", sortedParams) + $"&HashIV={HashIV}";

        // 3. URL Encode 處理並轉小寫
        string urlEncodedString = HttpUtility.UrlEncode(paramString).ToLower();

        // 4. SHA256 加密
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(urlEncodedString));
            string checkMacValue = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            return checkMacValue;
        }
    }
}
