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

namespace GoParkAPI.Controllers
{
    [EnableCors("EasyParkCors")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly EasyParkContext _context;
        private readonly Hash _hash;

        public CustomersController(EasyParkContext context, Hash hash)
        {
            _context = context;
            _hash = hash;
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

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<CustomerDTO> GetCustomer(int id)
        {
            var l = await _context.Car.Where(car=>car.UserId == id).FirstAsync();
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

        // PUT: api/Customers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //網址列id 第一個參數
        [HttpPut("{id}")]
        public async Task<string> PutCustomer(int id, CustomerDTO custDTO)
        {
            if (id != custDTO.UserId)
            {
                return "修改失敗";
            }
            Customer cust = await _context.Customer.FindAsync(id);
            Car l = await _context.Car.FindAsync(id);

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

            //密碼加密加鹽
            var (hashedPassword, salt) = _hash.HashPassword(custDTO.Password);

            custDTO.Password = hashedPassword;//加密
            custDTO.Salt = salt;//加鹽

            //custDTO.UserId = cust.UserId;//填入的id覆蓋原本預設的id 0
            _context.Customer.Add(cust);//加進資料庫
            await _context.SaveChangesAsync();//存檔
            Car car = new Car
            {
                CarId = 0,//填預設值 系統會覆蓋
                LicensePlate = custDTO.LicensePlate,//填入的車牌
                UserId = cust.UserId,//對應已經覆蓋的id
                IsActive = true,//填預設值
            };
            _context.Car.Add(car);//加進資料庫
            await _context.SaveChangesAsync();//存檔


            return CreatedAtAction("GetCustomer", new { id = custDTO.UserId }, custDTO);


            //return custDTO;
        }

        [HttpPost("login")]
        public IActionResult Login(LoginsDTO login)

        {
            var member = _context.Customer.Where(m => m.Email.Equals(login.Email)).SingleOrDefault();
            if (member != null)
            {
                // 從數據庫中獲取已加密的密碼和鹽值
                var hashedPassword = member.Password;
                var salt = member.Salt;
                var isPasswordValid = _hash.VerifyPassword(login.Password, hashedPassword, salt);

                if (!isPasswordValid)
                {
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                // 其他登入邏輯

                return Ok(new { Message = "Login successful!" });
            }

            return Ok(new { Message = "查無此帳號" });

        }

        //[HttpPost]
        //public async Task<ActionResult<CustomerDTO>> PostCustomer(CustomerDTO custDTO)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    try
        //    {
        //        Car car = new Car
        //        {
        //            CarId = 0,
        //            LicensePlate = custDTO.LicensePlate,
        //            UserId = custDTO.UserId,
        //            IsActive = true,
        //        };
        //        Customer cust = new Customer
        //        {
        //            UserId = custDTO.UserId,
        //            Username = custDTO.Username,
        //            Password = custDTO.Password,
        //            Salt = custDTO.Salt,
        //            Email = custDTO.Email,
        //            Phone = custDTO.Phone,
        //        };

        //        _context.Customer.Add(cust);
        //        _context.Car.Add(car);
        //        await _context.SaveChangesAsync();

        //        custDTO.UserId = cust.UserId;
        //        return custDTO;
        //    }
        //    catch (Exception ex)
        //    {
        //        // 記錄錯誤
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

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