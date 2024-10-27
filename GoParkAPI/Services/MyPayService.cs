using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using Humanizer;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Text;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;

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


        //------------------ 月租支付完成表單建立開始---------------------------------
        public async Task<(bool success, string message)> UpdatePaymentStatusAsync(string orderId)
        {
            var rentalRecord = await _context.MonthlyRental
                .FirstOrDefaultAsync(r => r.TransactionId == orderId);

            if (rentalRecord == null)
            {
                return (false, "找不到對應的租賃紀錄");
            }

            var parkLot = await _context.ParkingLots
                .FirstOrDefaultAsync(p => p.LotId == rentalRecord.LotId);

            if (parkLot == null)
            {
                return (false, "找不到對應的車位");
            }

            if (parkLot.MonRentalSpace <= 0 || parkLot.ValidSpace <= 0)
            {
                return (false, "車位不足，無法更新支付狀態");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                rentalRecord.PaymentStatus = true;
                parkLot.MonRentalSpace -= 1;
                parkLot.ValidSpace -= 1;

                var dealRecord = new DealRecord
                {
                    CarId = rentalRecord.CarId,
                    Amount = rentalRecord.Amount,
                    PaymentTime = DateTime.Now,
                    ParkType = "monthlyRental"
                };

                await _context.DealRecord.AddAsync(dealRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "支付狀態已更新");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("更新支付狀態失敗");
                return (false, "更新支付狀態失敗，請稍後再試");
            }
        }

        //------------------ 月租支付完成表單建立開始---------------------------------


        //------------------ 檢測預定金額和每個小時的時間是否相符開始 ---------------------------

        public async Task<bool> ValidateDayMoney(int lotId, int weekDay)
        {
            var park = await _context.ParkingLots
                .FirstOrDefaultAsync(r => r.LotId == lotId);
            if (park == null)
            {
                throw new Exception("此停車場並不存在");

            }
            if (park.WeekdayRate != weekDay)
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

        //------------------ 預約支付完成表單建立開始---------------------------------
        public async Task<(bool success, string message)> UpdateResPayment(string orderId)
        {
            var rentalRecord = await _context.Reservation
                .FirstOrDefaultAsync(r => r.TransactionId == orderId);

            if (rentalRecord == null)
            {
                return (false, "找不到對應的租賃紀錄");
            }

            var parkLot = await _context.ParkingLots
                .FirstOrDefaultAsync(p => p.LotId == rentalRecord.LotId);

            if (parkLot == null)
            {
                return (false, "找不到對應的車位");
            }

            if (parkLot.ValidSpace <= 0)
            {
                return (false, "車位不足，無法更新支付狀態");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                rentalRecord.PaymentStatus = true;
                parkLot.ValidSpace -= 1;

                var dealRecord = new DealRecord
                {
                    CarId = rentalRecord.CarId,
                    Amount = 3000,
                    PaymentTime = DateTime.Now,
                    ParkType = "margin"
                };

                await _context.DealRecord.AddAsync(dealRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "支付狀態已更新");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("更新支付狀態失敗");
                return (false, "更新支付狀態失敗，請稍後再試");
            }
        }

        //------------------ 預約支付完成表單建立開始---------------------------------

        //--------------------- 預約表單開始 ------------------------

        public Reservation ResMapDtoToModel(PaymentRequestDto dto)
        {
            
            return new Reservation
            {
                CarId = dto.CarId,
                LotId = dto.LotId,
                ResTime=DateTime.Now,
                StartTime = dto.StartTime,
                PaymentStatus = false,
                TransactionId = dto.OrderId
            };

        }
        //--------------------- 預約表單結束 ------------------------
    }
}
