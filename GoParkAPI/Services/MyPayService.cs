using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using Humanizer;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Text;

namespace GoParkAPI.Services
{
    public class MyPayService
    {
        private readonly EasyParkContext _context;
        public MyPayService(EasyParkContext context)
        {
            _context = context;
        }
        //--------------------- 月租方案開始 ------------------------

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
                CarId = dto.CarId,
                LotId = dto.LotId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(rentalMonths),
                Amount = dto.Amount,
                PaymentStatus = false,
                TransactionId = dto.OrderId
            };

        }
        //--------------------- 月租方案結束 ------------------------




        //------------------ 檢測月租方案是否相符開始 ---------------------
        public async Task<bool> ValidatePayment(int LotId ,string planId, int paidAmount)
        {
            var park = await _context.ParkingLots
                .FirstOrDefaultAsync(r => r.LotId == LotId);
            
            if (park == null)
            {
                throw new Exception("此停車場並不存在");
            }
            var (rentalMonths, discount) = planId switch
            {
                "oneMonth" => (1, 0),           // 無折扣
                "threeMonths" => (3, 0.10m),      // 5% 折扣
                "sixMonths" => (6, 0.15m),        // 10% 折扣
                "twelveMonths" => (12, 0.20m),    // 15% 折扣
                _ => throw new ArgumentException("無效的方案 ID")
            };

            // 根據租賃月數計算未折扣的總金額
            decimal originalAmount = park.MonRentalRate * rentalMonths;

            // 計算折扣後的金額
            decimal discountedAmount = originalAmount * (1 - discount);

            // 驗證支付的金額是否正確
            if (Math.Round(discountedAmount) != paidAmount) // 四捨五入取整數
            {
                Console.WriteLine($"{park.LotName} 的方案 {planId} 金額比對錯誤：應支付 {Math.Round(discountedAmount)}，實際支付 {paidAmount}");
                return false;
            }

            Console.WriteLine($"{park.LotName} 的方案 {planId} 金額比對成功：支付金額 {paidAmount}");
            return true;

        }
        //------------------ 檢測月租方案是否相符結束 ---------------------

        //------------------ 檢測預定金額和每個小時的時間是否相符開始 ---------------------------

        public async Task<bool> ValidateDayMoney(int lotId,int weekDay)
        {
            var park = await _context.ParkingLots
                .FirstOrDefaultAsync(r => r.LotId == lotId);
            if(park ==null)
            {
                throw new Exception("此停車場並不存在");
          
            }
            if(park.WeekdayRate != weekDay)
            {
                Console.WriteLine($"{park.LotName}的每小時金額比對錯誤");
                return false;
            }
            else
            {
                return true;
            }
            
        }
        //------------------ 檢測預定金額和每個小時的時間是否相符結束 ---------------------------


    }
}
