using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GoParkAPI.Services
{
    public class LinePayService
    {
        private readonly string channelId = "1657306405";
        private readonly string channelSecretKey = "720c8af4d271ce2fef3535b8821d9e8e";
        private readonly string linePayBaseApiUrl = "https://sandbox-api-pay.line.me";

        private readonly HttpClient _client;
        private readonly JsonProvider _jsonProvider;
        private readonly EasyParkContext _context;

        public LinePayService(HttpClient client, JsonProvider jsonProvider, EasyParkContext context)
        {
            _client = client;
            _jsonProvider = jsonProvider;
            _context = context;
        }

        private void AddLinePayHeaders(HttpRequestMessage request, string nonce, string signature)
        {
            request.Headers.Add("X-LINE-ChannelId", channelId);
            request.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            request.Headers.Add("X-LINE-Authorization", signature);
        }

        public async Task<PaymentResponseDto> SendPaymentRequest(PaymentRequestDto dto)
        {
            var json = _jsonProvider.Serialize(dto);
            var nonce = Guid.NewGuid().ToString();
            var requestUrl = "/v3/payments/request";
            var signature = SignatureProvider.HMACSHA256(channelSecretKey, channelSecretKey + requestUrl + json + nonce);

            var request = new HttpRequestMessage(HttpMethod.Post, linePayBaseApiUrl + requestUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            AddLinePayHeaders(request, nonce, signature);

            var response = await _client.SendAsync(request);
  

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"LinePay API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            return _jsonProvider.Deserialize<PaymentResponseDto>(await response.Content.ReadAsStringAsync());
        }

        public async Task<PaymentConfirmResponseDto> ConfirmPayment(string transactionId, string orderId, PaymentConfirmDto dto)
        {
            var json = _jsonProvider.Serialize(dto);
            var nonce = Guid.NewGuid().ToString();
            var requestUrl = $"/v3/payments/{transactionId}/confirm";
            var signature = SignatureProvider.HMACSHA256(channelSecretKey, channelSecretKey + requestUrl + json + nonce);

            var request = new HttpRequestMessage(HttpMethod.Post, linePayBaseApiUrl + requestUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            AddLinePayHeaders(request, nonce, signature);

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"LinePay Confirm API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            return _jsonProvider.Deserialize<PaymentConfirmResponseDto>(await response.Content.ReadAsStringAsync());
        }

        public async Task TransactionCancel(string transactionId)
        {
            Console.WriteLine($"訂單 {transactionId} 已取消");
            await Task.CompletedTask;
        }


        public MonthlyRental MapDtoToModel(PaymentRequestDto dto)
        {
            // 根據方案 ID 動態設置結束日期
            int rentalMonths = dto.PlanId switch
            {
                "oneMonth" => 1,
                "threeMonths" => 3,
                "sixMonths" => 6,
                "twelveMonths" => 12,
                _ => throw new ArgumentException("Invalid PlanId")
            };

            return new MonthlyRental
            {

                CarId = 1,
                LotId = 1,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(rentalMonths),
                Amount = dto.Amount,
                PaymentStatus = false
            };
            
        }
        //public async Task SaveMonthlyRental(MonthlyRental rental)
        //{
        //    _dbContext.MonthlyRental.Add(rental);
        //    await _dbContext.SaveChangesAsync();
        //}
        public bool ValidatePayment(string planId, decimal paidAmount)
        {
            if (!PlanData.Plans.TryGetValue(planId, out var plan))
            {
                Console.WriteLine($"方案ID不存在: {planId}");
                return false; // 方案 ID 不存在
            }

            Console.WriteLine($"查找到的方案: {plan.Name}, 金額: {plan.Price}");

            if (plan.Price != paidAmount)
            {
                Console.WriteLine("金額不符。");
                return false;
            }

            return true; // 驗證通過
        }

    }
}
