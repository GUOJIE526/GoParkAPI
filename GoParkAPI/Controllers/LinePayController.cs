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
        private readonly MyPayService _myPayService;
        private readonly EasyParkContext _context;
        private readonly MailService _sentmail;
        public LinePayController(LinePayService linePayService, EasyParkContext context, MailService sentmail, MyPayService myPayService)
        {
            _linePayService = linePayService;
            _context = context;
            _sentmail = sentmail;
            _myPayService = myPayService;
        }

        // ------------------------ 驗證月租方案是否相符開始 -------------------------------

        [HttpPost("Validate")]
        public async Task<IActionResult> ValidatePayment([FromBody] PaymentValidationDto dto)
        {
            try
            {
                // 驗證基本資料
                if (dto.LotId <= 0 || string.IsNullOrEmpty(dto.PlanId) || dto.Amount <= 0)
                {
                    return BadRequest(new { message = "無效的停車場 ID、方案 ID 或金額。" });
                }

                // 呼叫服務層進行驗證
                bool isValid = await _myPayService.ValidatePayment(dto.LotId, dto.PlanId, dto.Amount);

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

        // ------------------------ 驗證月租方案是否相符結束 -------------------------------

        // ------------------------ 發送月租付款申請開始 -------------------------------

        [HttpPost("Create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequestDto dto)
        {
            try
            {
                // 使用 LinePay 服務發送支付請求
                var paymentResponse = await _linePayService.SendPaymentRequest(dto);

                // 從資料庫查詢該 CarId 的所有租賃記錄
                var existingRentals = await _context.MonthlyRental
                    .Where(r => r.CarId == dto.CarId)
                    .ToListAsync();

                // 將 DTO 映射為 MonthlyRental 模型
                MonthlyRental rentalRecord = _myPayService.MapDtoToModel(dto, existingRentals);

                // 將租賃記錄新增到資料庫
                await _context.MonthlyRental.AddAsync(rentalRecord);

                // 保存變更
                await _context.SaveChangesAsync();

                // 回傳支付結果
                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤：{ex.Message}");

                // 回傳錯誤回應
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "處理支付時發生錯誤。" });
            }

        }

        // ------------------------ 發送月租付款申請結束 -------------------------------

        // ------------------------ 完成月租付並建立付款記錄開始 -------------------------------


        [HttpPost("UpdatePaymentStatus")]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] UpdatePaymentStatusDTO dto)
        {
            // 1. 查詢 Customer 資料
            var customer = await _context.MonthlyRental
                .Where(m => m.TransactionId == dto.OrderId)
                .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
                .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }

            // 2. 更新支付狀態
            var (success, message) = await _myPayService.UpdatePaymentStatusAsync(dto.OrderId);

            if (!success)
            {
                return NotFound(new { success = false, message });
            }

            // 3. 發送成功通知郵件
            string subject = "MyGoParking!";
            string emailMessage = $@"<p>親愛的 {customer.Username}：</p>
                                    <p>敬祝順利，感謝申請月租！</p>
                                    <p>MyGoParking 團隊 敬上</p>";

            try
            {
                await _sentmail.SendEmailAsync(customer.Email, subject, emailMessage); // 發送信件
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


        // ------------------------ 完成月租付並建立付款記錄結束 -------------------------------


        // ------------------------ 驗證預約停車的金額是否相符開始 -------------------------------

        [HttpPost("ValidateDay")]
        public async Task<IActionResult> ValidateDayPayment([FromBody] PaymentValidationDayDto request)
        {
            if (request == null || request.lotId <= 0 || request.Amount < 0)
            {
                return BadRequest(new { message = "無效的請求資料。" });
            }
            try
            {
                bool isValid = await _myPayService.ValidateDayMoney(request.lotId, request.Amount);

                if (!isValid)
                {
                    return BadRequest(new { message = "方案或金額驗證失敗。" });
                }
                Console.WriteLine("金額正確通過");
                return Ok(new { isValid = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"伺服器錯誤: {ex.Message}");
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }

        // ------------------------ 驗證預約停車的金額是否相符結束 -------------------------------


        //------------------------- 從前台接收lotId資訊，然後從後台返回資料-------------------------

        [HttpPost("ListenLotId")]
        public async Task<IActionResult> ListenLotId([FromBody] ListenLotDTO dto)
        {
            try
            {

                var lotId = dto.LotId;
                if (lotId == null)
                {
                    return NotFound(new { message = "LotId 不存在於 Session" });
                }

                // 2. 根據 LotId 查詢停車場資訊
                var park = await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == lotId);
                if (park == null)
                {
                    return NotFound(new { message = "找不到對應的停車場" });
                }

                // 3. 回傳停車場資訊
                return Ok(new
                {
                    message = "成功取得停車場資訊",
                    lotName = park.LotName,
                    lotType = park.Type,
                    lotLocation = park.Location,
                    lotValid = park.ValidSpace,
                    lotWeek = park.WeekdayRate,
                    lotTel = park.Tel,
                    lotLatitude = park.Latitude,
                    lotLongitude = park.Longitude,
                    lotResDeposit = park.ResDeposit,
                });
            }
            catch (Exception ex)
            {
                // 4. 捕捉例外狀況並回傳 500 錯誤
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }

        }


        //------------------------------ 預約支付請求開始 ------------------------------------

        [HttpPost("CreateDay")]
        public async Task<IActionResult> CreateDay([FromBody] PaymentRequestDto dto)
        {
            try
            {
                // 使用 LinePay 服務發送支付請求
                var paymentResponse = await _linePayService.SendPaymentRequest(dto);

                Reservation rentalRecord = _myPayService.ResMapDtoToModel(dto);

                // 將租賃記錄新增到資料庫
                await _context.Reservation.AddAsync(rentalRecord);

                // 保存變更
                await _context.SaveChangesAsync();

                // 回傳支付結果
                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤：{ex.Message}");

                // 回傳錯誤回應
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "處理支付時發生錯誤。" });
            }

        }

        // ------------------------ 預約支付請求結束 -------------------------------

        //------------------------- 預約完成表單建立開始 -------------------------------

        [HttpPost("UpdateResPayment")]
        public async Task<IActionResult> UpdateResPayment([FromBody] UpdatePaymentStatusDTO dto)
        {
            // 1. 查詢 Customer 資料
            var customer = await _context.Reservation
                .Where(m => m.TransactionId == dto.OrderId)
                .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
                .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }

            // 2. 更新支付狀態
            var (success, message) = await _myPayService.UpdateResPayment(dto.OrderId);

            if (!success)
            {
                return NotFound(new { success = false, message });
            }
            // 3.發送成功通知郵件
            string subject = "MyGoParking!";
            string emailMessage = $@"<p>親愛的 {customer.Username}：</p>
                                    <p>敬祝順利，感謝預約！</p>
                                    <p>MyGoParking 團隊 敬上</p>";

            try
            {
                await _sentmail.SendEmailAsync(customer.Email, subject, emailMessage); // 發送信件
                Console.WriteLine($"成功發送郵件至 {customer.Email}");
            }
            catch (Exception ex)
            {
                // 捕捉並記錄郵件發送錯誤
                Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
            }

            // 5. 支付成功回應
            return Ok(new { success = true, message = "支付狀態更新成功並已發送通知。" });
        }


        //------------------------- 預約完成表單建立結束 -------------------------------


        //-------------------------------------------------------------------------------------------

        [HttpPost("Confirm")]
        public async Task<PaymentConfirmResponseDto> ConfirmPayment([FromQuery] string transactionId, [FromQuery] string orderId, PaymentConfirmDto dto)
        {

            return await _linePayService.ConfirmPayment(transactionId, orderId, dto);
        }



        [HttpGet("Cancel")]
        public async void CancelTransaction([FromQuery] string transactionId)
        {
            _linePayService.TransactionCancel(transactionId);
        }


        // ------------------------ 發送月租付款申請開始 -------------------------------

        //------------------------- 從前台接收UserId資訊，然後從後台返回資料-------------------------

        [HttpPost("ListenUserId")]
        public async Task<IActionResult> ListenUserId([FromBody] ListenUserDTO dto)
        {
            try
            {
                if (dto == null || dto.UserId <= 0)
                {
                    return BadRequest(new { message = "無效的 UserId。" });
                }

                // 獲取當前時間
                var currentTime = DateTime.Now;

                // 查詢車輛資料並關聯 EntryExitManagement 資料表
                var userCars = await _context.Customer
                    .Where(u => u.UserId == dto.UserId)
                    .Join(_context.Car,
                          u => u.UserId,
                          c => c.UserId,
                          (u, c) => new { u.UserId, Car = c })
                    .Join(_context.EntryExitManagement,
                          car => car.Car.CarId,
                          entry => entry.CarId,
                          (car, entry) => new
                          {
                              car.UserId,
                              car.Car.CarId,
                              car.Car.LicensePlate,
                              entry.Parktype,
                              entry.EntryTime
                          })
                    .Where(e => e.Parktype == "reservation") // 過濾出 ParkType 為 reservation 的紀錄
                    .ToListAsync();

                if (!userCars.Any())
                {
                    return NotFound(new { message = "找不到對應的用戶或車輛資料。" });
                }

                var carList = userCars.Select(car => new
                {
                    carId = car.CarId,
                    licensePlate = car.LicensePlate,
                    entryTime = car.EntryTime,
                    // 使用 Math.Ceiling 強制進位
                    hoursParked = car.EntryTime.HasValue
                    ? Math.Ceiling((currentTime - car.EntryTime.Value).TotalHours)
                    : (double?)null
                }).ToList();

                // 查詢可用的優惠券資料
                var userCoupons = await _context.Customer
                    .Where(u => u.UserId == dto.UserId)
                    .Join(_context.Coupon,
                          u => u.UserId,
                          coup => coup.UserId,
                          (u, coup) => new
                          {
                              UserId = u.UserId,
                              CouponId = coup.CouponId,
                              Amount = coup.DiscountAmount,
                              IsUsed = coup.IsUsed,
                              ValidFrom = coup.ValidFrom,
                              ValidUntil = coup.ValidUntil
                          })
                    .Where(c => !c.IsUsed &&
                                currentTime >= c.ValidFrom &&
                                currentTime <= c.ValidUntil)
                    .ToListAsync();

                var couponList = userCoupons.Select(coupon => new
                {
                    couponId = coupon.CouponId,
                    amount = coupon.Amount
                }).ToList();

                // 檢查是否有可用的優惠券
                if (!couponList.Any())
                {
                    return NotFound(new { message = "沒有可用的優惠券。" });
                }

                // 回傳車輛和優惠券資料
                return Ok(new
                {
                    userId = userCars.First().UserId,
                    cars = carList, 
                    coupons = couponList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }




        //------------------------------ 預約支付請求開始 ------------------------------------
    }
}
