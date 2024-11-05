using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Azure.Identity;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.ComponentModel.DataAnnotations;
using GoParkAPI.DTO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using NuGet.Protocol.Plugins;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace GoParkAPI.Controllers
{
    //[EnableCors("EasyParkCors")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly EasyParkContext _context;
        private readonly pwdHash _hash;
        private readonly MailService _sentmail;


        public CustomersController(EasyParkContext context, pwdHash hash, MailService sentmail)
        {
            _context = context;
            _hash = hash;
            _sentmail = sentmail;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<IEnumerable<CustomerDTO>> GetCustomer()
        {
            return _context.Customer.Select(cust => new CustomerDTO
            {
                UserId = cust.UserId,
                Username = cust.Username,
                Password = cust.Password,
                Salt = cust.Salt,
                Email = cust.Email,
                Phone = cust.Phone,
                LicensePlate = _context.Car.Where(car => car.UserId == cust.UserId).Select(car => car.LicensePlate).FirstOrDefault()
            });
        }

        //GET: api/Customers/5
        [HttpGet("info{id}")]
        public async Task<CustomerDTO> GetCustomer(int id)
        {
            var l = await _context.Car.Where(car => car.UserId == id).FirstAsync();
            string cnum = l.LicensePlate;
            var customer = await _context.Customer.FindAsync(id);
            CustomerDTO custDTO = new CustomerDTO
            {
                UserId = customer.UserId,
                Username = customer.Username,
                Password = customer.Password,
                Salt = customer.Salt,
                Email = customer.Email,
                Phone = customer.Phone,
                LicensePlate = cnum
            };

            if (customer == null)
            {
                return null;
            }

            return custDTO;
        }

        // PUT: api/Customers/id5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //網址列id 第一個參數
        [HttpPut("id{id}")]
        public async Task<string> PutCustomer(int id, CustomerDTO custDTO)
        {
            if (id != custDTO.UserId)
            {
                return "無法修改";
            }

            // 查找 Customer
            Customer cust = await _context.Customer.FindAsync(id);
            if (cust == null)
            {
                return "無法找到會員資料";
            }

            // 查找 Car，假設每個 Customer 對應一輛 Car
            Car car = await _context.Car.FirstOrDefaultAsync(c => c.UserId == id);
            if (car == null)
            {
                return "無法找到車輛資料";
            }



            // 密碼加密與加鹽
            var (hashedPassword, salt) = _hash.HashPassword(custDTO.Password);
            custDTO.Password = hashedPassword;
            custDTO.Salt = salt;

            // 更新 Customer 資料
            cust.Username = custDTO.Username;
            cust.Password = custDTO.Password; // 確保已經 hash 過密碼
            cust.Salt = custDTO.Salt;
            cust.Email = custDTO.Email;
            cust.Phone = custDTO.Phone;

            // 更新 Car 的 LicensePlate
            car.LicensePlate = custDTO.LicensePlate;

            // 設定狀態為已修改
            _context.Entry(cust).State = EntityState.Modified;
            _context.Entry(car).State = EntityState.Modified;

            try
            {
                // 儲存修改
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return "修改失敗";
                }
                else
                {
                    throw;
                }
            }

            return "修改成功";
        }

        [HttpPut("password{id}")]
        public async Task<IActionResult> ChangePassword(int id, ChangePswDTO pswDto)
        {
            // 根據傳入的 id 找到用戶
            var customer = await _context.Customer.FindAsync(id);
            if (customer == null)
            {
                return NotFound("用戶不存在");
            }


            // 驗證舊密碼是否正確
            if (!_hash.VerifyPassword(pswDto.OldPassword, customer.Password, customer.Salt))
            {
                return BadRequest("舊密碼不正確");
            }

            // 使用新的密碼和鹽值覆蓋舊密碼
            var (newHashedPassword, newSalt) = _hash.HashPassword(pswDto.NewPassword);
            customer.Password = newHashedPassword;
            customer.Salt = newSalt;

            // 設置資料庫狀態為已修改
            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                // 儲存更改
                await _context.SaveChangesAsync();
                return Ok("密碼更新成功");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "更新失敗，請稍後再試");
            }
        }



        // POST: api/Customers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754

        [HttpPost("sign")]
        public async Task<ActionResult> PostCustomer(CustomerDTO custDTO)
        {
            // 檢查是否已有相同的Email或車牌
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.Email == custDTO.Email);
            var existingCar = await _context.Car.FirstOrDefaultAsync(c => c.LicensePlate == custDTO.LicensePlate);

            // 如果用戶不存在，並且車牌不存在
            if (customer == null && existingCar == null)
            {
                // 密碼加密與加鹽
                var (hashedPassword, salt) = _hash.HashPassword(custDTO.Password);
                custDTO.Password = hashedPassword;
                custDTO.Salt = salt;

                // 創建新用戶
                Customer cust = new Customer
                {
                    Username = custDTO.Username,
                    Password = custDTO.Password,
                    Salt = custDTO.Salt,
                    Email = custDTO.Email,
                    Phone = custDTO.Phone
                };

                // 將新用戶添加到資料庫
                _context.Customer.Add(cust);
                await _context.SaveChangesAsync();

                // 創建用戶的車輛資料
                Car car = new Car
                {
                    LicensePlate = custDTO.LicensePlate,
                    UserId = cust.UserId, // 使用剛創建的用戶ID
                    IsActive = true
                };

                // 將車輛資料添加到資料庫
                _context.Car.Add(car);
                await _context.SaveChangesAsync();

                // 發送歡迎郵件
                if (!string.IsNullOrEmpty(custDTO.Email))
                {
                    string subject = "歡迎加入 MyGoParking!";
                    string message = $"<p>親愛的用戶：感謝您註冊，您已成功加入！<br>敬祝順利<br>mygoParking團隊</p>";

                    try
                    {
                        await _sentmail.SendEmailAsync(custDTO.Email, subject, message);
                    }
                    catch (Exception ex)
                    {
                        // 錯誤處理
                        Console.WriteLine($"發送郵件失敗: {ex.Message}");
                    }
                }

                // 回傳註冊成功的完整用戶資料給前端
                return Ok(new
                {
                    exit = true,
                    message = "註冊成功!",
                    userId = cust.UserId,
                    username = cust.Username,
                    email = cust.Email,
                    phone = cust.Phone,
                    licensePlate = car.LicensePlate,
                    password = cust.Password, // 回傳加密後的密碼
                    salt = cust.Salt
                });
            }
            // 檢查帳號是否已存在
            else if (customer != null)
            {
                return Ok(new { message = "此帳號已註冊!" });
            }
            // 檢查車牌是否已存在
            else if (existingCar != null)
            {
                return Ok(new { message = "此車牌已存在!" });
            }
            else
            {
                return Ok(new { message = "請洽客服人員!" });
            }
        }


        [HttpPost("forget")]
        public async Task<ActionResult<CustomerDTO>> ForgetPassword(string email, string newPassword)
        {
            // 檢查 Email 是否存在於系統中
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
            {
                return NotFound("該 Email 不存在");
            }

            // 生成一個密碼重置 token
            var token = Guid.NewGuid().ToString(); // 可考慮使用更安全的生成方法
            customer.Token = token;

            // 檢查新密碼是否與舊密碼相同
            if (customer.Password == _hash.HashPassword(newPassword).Item1) // 使用哈希方法
            {
                return BadRequest("新密碼不能與舊密碼相同");
            }

            // 將新密碼進行哈希處理並更新
            var (hashedPassword, salt) = _hash.HashPassword(newPassword);
            customer.Password = hashedPassword; // 更新哈希後的新密碼
            customer.Salt = salt; // 更新鹽值

            // 設定 token 有效期
            //customer.TokenExpiration = DateTime.UtcNow.AddHours(1);

            // 儲存 token 到數據庫
            _context.Customer.Update(customer);
            await _context.SaveChangesAsync();

            // 生成重置密碼的鏈接
            string resetLink = $"http://localhost:5173/signIn?token={token}&email={email}";

            // 準備郵件的標題和內容
            string subject = "MyGoParking 密碼重置";
            string message = $"<p>親愛的用戶：<br>請點擊以下連結確認您的新密碼：</p>" + $"<a href=\"{resetLink}\">確認新密碼: \"{newPassword}\"</a><br>" + "<p>此鏈接將在1小時內過期。<br>mygoParking團隊</p>";

            try
            {
                // 發送郵件
                await _sentmail.SendEmailAsync(email, subject, message);
                return Ok("密碼重置郵件已發送，請檢查您的郵箱。");
            }
            catch (Exception ex)
            {
                // 發送郵件失敗的錯誤處理
                Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
                return StatusCode(500, "發送郵件時發生錯誤");
            }
        }



        [HttpPost("login")]
        public IActionResult Login(LoginsDTO login)

        {
            bool exit = false;
            string message = "";
            int UserId = 0;
            var member = _context.Customer.Where(m => m.Email.Equals(login.Email)).SingleOrDefault();
            
            if (member != null)
            {
               
                // 從數據庫中獲取已加密的密碼和鹽值
                var hashedPassword = member.Password;
                var salt = member.Salt;
                var isPasswordValid = _hash.VerifyPassword(login.Password, hashedPassword, salt);

                if (!isPasswordValid)
                {
                    
                    message = "登入失敗";
                }
                else
                {
                    exit = true;
                    message = "登入成功";
                    UserId = member.UserId;
                }
            }
            else
            {
                exit = false;
                message = "無此帳號";
            }
            ExitDTO exitDTO = new ExitDTO
            {
                Exit = exit,
                UserId = UserId,
                Message = message,
            };
            return Ok(exitDTO);

        }

        [HttpPost("coupon")]
        public async Task<ActionResult<CouponDTO>> AddCoupon(CouponDTO coupDTO)
        {
            var userId = await _context.Customer.FirstOrDefaultAsync(u => u.UserId == coupDTO.UserId);

            if (userId != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Coupon coup = new Coupon
                    {
                        CouponId = coupDTO.CouponId,
                        CouponCode = coupDTO.CouponCode,
                        DiscountAmount = coupDTO.DiscountAmount,
                        ValidFrom = coupDTO.ValidFrom,
                        ValidUntil = coupDTO.ValidUntil,
                        IsUsed = coupDTO.IsUsed,
                        UserId = coupDTO.UserId
                    };
                    _context.Coupon.Add(coup);//加進資料庫 
                }
                await _context.SaveChangesAsync();//存檔 
                return Ok(new { message = "成功領取三張優惠券!" });
            }
            else if(userId == null)
            {
                return Ok(new { message = "領取失敗,您尚未註冊或登入"});
            }
            return Ok(new { message = "領取失敗, 請洽客服人員" });
        }



        // DELETE: api/Customers/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCustomer(int id)
        //{
        //    var customer = await _context.Customer.FindAsync(id);
        //    if (customer == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Customer.Remove(customer);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool CustomerExists(int id)
        {
            return _context.Customer.Any(e => e.UserId == id);
        }
    }

}
