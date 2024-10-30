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

        //------------------------------ 檢測停車開始 ------------------------------------
        [HttpPost("testddd")]
        public async Task<IActionResult> testddd([FromBody] ListenCarDTO dto)
        {
            // 根據 licensePlate 查找對應的 Car 物件
            var car = await _context.Car
                .FirstOrDefaultAsync(c => c.LicensePlate == dto.licensePlate);

            if (car == null)
            {
                // 如果找不到對應的車輛，回傳錯誤訊息
                return NotFound(new { success = false, message = "未找到該車輛。" });
            }

            // 根據 CarId 比對 EntryExitManagement 中的記錄
            var entryExitRecord = await _context.EntryExitManagement
                .Where(eem => eem.CarId == car.CarId)
                .OrderByDescending(eem => eem.EntryTime) // 取最新的進入記錄
                .FirstOrDefaultAsync();

            if (entryExitRecord == null)
            {
                // 如果找不到對應的出入管理記錄，回傳錯誤訊息
                return NotFound(new { success = false, message = "未找到該車輛的出入管理記錄。" });
            }

            // 根據 LotId 取得對應的停車場費率 (WeekdayRate)
            var parkingLot = await _context.ParkingLots
                .FirstOrDefaultAsync(lot => lot.LotId == entryExitRecord.LotId);

            if (parkingLot == null)
            {
                // 如果找不到對應的停車場，回傳錯誤訊息
                return BadRequest(new { success = false, message = "找不到對應的停車場資料。" });
            }

            // 設定 LicensePlateKeyinTime 為目前時間
            entryExitRecord.LicensePlateKeyinTime = DateTime.Now;

            // 計算停留時間（小時）
            TimeSpan? duration = entryExitRecord.LicensePlateKeyinTime - entryExitRecord.EntryTime;

            if (!duration.HasValue)
            {
                return BadRequest(new { success = false, message = "無法計算停留時間。" });
            }

            var durationHours = (int)Math.Ceiling(duration.Value.TotalHours); // 進位處理小時數

            // 使用停車場的 WeekdayRate 計算金額
            var amount = durationHours * parkingLot.WeekdayRate;

            // 取得目前時間
            var TimeNow = DateTime.Now;

            // 根據 Car 的 UserId 查找符合條件的有效 Coupons
            var coupons = await _context.Coupon
                .Where(c => c.UserId == car.UserId &&
                            !c.IsUsed &&
                            TimeNow >= c.ValidFrom &&
                            TimeNow <= c.ValidUntil)
                .OrderBy(c => c.ValidUntil) // 以 ValidUntil 升序排序
                .ToListAsync();

            // 將符合條件的 CouponId 回傳
            var couponIds = coupons.Select(c => new
            {
                CouponId = c.CouponId,
                couponAmount = c.DiscountAmount,
                EndTime = c.ValidUntil.Date.ToString("yyyy-MM-dd") // 日期格式化為指定字串格式
            }).ToList();

            // 回傳結果
            return Ok(new
            {
                CarId = car.CarId,
                LicensePlate = car.LicensePlate,
                EntryTime = entryExitRecord.EntryTime?.ToString("yyyy-MM-dd HH:mm") ?? "N/A", // 檢查是否為 null
                LicensePlateKeyinTime = entryExitRecord.LicensePlateKeyinTime?.ToString("yyyy-MM-dd HH:mm") ?? "N/A", // 檢查是否為 null
                DurationHours = durationHours,
                PlateAmount = amount,
                couponIds
            });
        }



    }
}
