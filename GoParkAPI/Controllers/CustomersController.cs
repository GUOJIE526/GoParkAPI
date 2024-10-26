﻿using System;
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

namespace GoParkAPI.Controllers
{
    [EnableCors("EasyParkCors")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly EasyParkContext _context;
        private readonly Hash _hash;
        private readonly MailService _sentmail;


        public CustomersController(EasyParkContext context, Hash hash, MailService sentmail)
        {
            _context = context;
            _hash = hash;
            _sentmail = sentmail;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<IEnumerable<CustomerDTO>> GetCustomer()
        {
            return _context.Customers.Select(cust => new CustomerDTO
            {
                UserId = cust.UserId,
                Username = cust.Username,
                Password = cust.Password,
                Salt = cust.Salt,
                Email = cust.Email,
                Phone = cust.Phone,
                LicensePlate = _context.Cars.Where(car => car.UserId == cust.UserId).Select(car => car.LicensePlate).FirstOrDefault()
            });
        }

        //GET: api/Customers/5
        [HttpGet("info{id}")]
        public async Task<CustomerDTO> GetCustomer(int id)
        {
            var l = await _context.Cars.Where(car => car.UserId == id).FirstAsync();
            string cnum = l.LicensePlate;
            var customer = await _context.Customers.FindAsync(id);
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

        // PUT: api/Customers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //網址列id 第一個參數
        [HttpPut("id{id}")]
        public async Task<string> PutCustomer(int id, CustomerDTO custDTO)
        {
            if (id != custDTO.UserId)
            {
                return "無法修改";
            }
            Customer cust = await _context.Customers.FindAsync(id);
            Car l = await _context.Cars.FindAsync(id);

            if (cust == null)
            {
                return "修改失敗";
            }
            cust.Password = custDTO.Password;
            cust.Email = custDTO.Email;
            cust.Phone = custDTO.Phone;
            l.LicensePlate = custDTO.LicensePlate;
            _context.Entry(cust).State = EntityState.Modified;
            try
            {
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

        // POST: api/Customers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CustomerDTO>> PostCustomer(CustomerDTO custDTO)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == custDTO.Email);
            if (customer == null)
            {
                //密碼加密加鹽
                var (hashedPassword, salt) = _hash.HashPassword(custDTO.Password);
                custDTO.Password = hashedPassword;//加密
                custDTO.Salt = salt;//加鹽

                Customer cust = new Customer
                {
                    //都允許空值 所以直接帶入填入資料 沒填的=>空值
                    UserId = custDTO.UserId,
                    Username = custDTO.Username,
                    Password = custDTO.Password,
                    Salt = custDTO.Salt,
                    Email = custDTO.Email,
                    Phone = custDTO.Phone,
                };
                _context.Customers.Add(cust);//加進資料庫 
                await _context.SaveChangesAsync();//存檔
                Car car = new Car
                {
                    CarId = 0,//填預設值 系統會覆蓋
                    LicensePlate = custDTO.LicensePlate,//填入的車牌
                    UserId = cust.UserId,//對應已經覆蓋的id
                    IsActive = true,//填預設值
                };
                _context.Cars.Add(car);//加進資料庫
                await _context.SaveChangesAsync();//存檔

                // 檢查 Email 是否存在並發送確認郵件
                if (!string.IsNullOrEmpty(custDTO.Email))
                {
                    string subject = "歡迎加入 MyGoParking!";
                    string message = $"<p>親愛的用戶： 敬祝順利 <br> mygoParking團隊 </p> "; // 郵件內容

                    try
                    {
                        await _sentmail.SendEmailAsync(custDTO.Email, subject, message); // 發送信件
                    }
                    catch (Exception ex)
                    {
                        // 錯誤處理
                        Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
                    }
                }
                var id = await _context.Customers.FirstOrDefaultAsync(c => c.Email == custDTO.Email);
                //return CreatedAtAction("GetCustomer", new {  id });
                return Ok( new { message = id.UserId });
            }
            else
            {
                return Ok(new { message="此帳號已註冊!"});
            }
            
        }


        [HttpPost("reset")]
        public async Task<ActionResult<CustomerDTO>> ResetPassword(string email, string newPassword)
        {
            // 檢查 Email 是否存在於系統中
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
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
            _context.Customers.Update(customer);
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
            var member = _context.Customers.Where(m => m.Email.Equals(login.Email)).SingleOrDefault();

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
            exitDTO exitDTO = new exitDTO
            {
                exit = exit,
                UserId = UserId,
                message = message,
            };
            return Ok(exitDTO);

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
            return _context.Customers.Any(e => e.UserId == id);
        }
    }

}
