using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Azure.Core;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonthlyRentalsController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public MonthlyRentalsController(EasyParkContext context)
        {
            _context = context;
        }

        //先抓出該用戶註冊的車牌id
        private async Task<List<int>> GetUserCars(int userId)
        {
            return await _context.Cars
                .Where(car => car.UserId == userId)
                .Select(car => car.CarId)
                .ToListAsync();
        }

        // GET: api/MonthlyRentals
        [HttpGet]
        public async Task<IEnumerable<MonthlyRentalDTO>> GetMonthlyRentals(int userId, string? licensePlate)
        {
            //根據 userId抓出用戶的車牌id
            var userCars = await GetUserCars(userId);

            //篩選該用戶車牌的預訂資料
            var rentals = _context.MonthlyRentals
                .Where(rental => userCars.Contains(rental.CarId)) // 比對車牌號碼
                .Where(rental => string.IsNullOrEmpty(licensePlate) || rental.Car.LicensePlate == licensePlate) //若有填寫車牌則進一步篩選
                .Select(rental => new MonthlyRentalDTO
                {
                    renId = rental.RenId,
                    licensePlate = rental.Car.LicensePlate,
                    lotId = rental.LotId,
                    lotName = rental.Lot.LotName,
                    latitude = rental.Lot.Latitude,
                    longitude = rental.Lot.Longitude,
                    location = rental.Lot.Location,
                    district = rental.Lot.District,
                    startDate = rental.StartDate,
                    endDate = rental.EndDate,
                    amount = rental.Amount,  //付的總額

                });

            if (rentals == null)
            {
                return null;
            }
            return rentals;


        }

        // GET: api/MonthlyRentals/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<MonthlyRental>> GetMonthlyRental(int id)
        //{
        //    var monthlyRental = await _context.MonthlyRentals.FindAsync(id);

        //    if (monthlyRental == null)
        //    {
        //        return NotFound();
        //    }

        //    return monthlyRental;
        //}

        // PUT: api/MonthlyRentals/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMonthlyRental(int id, MonthlyRental monthlyRental)
        {
            if (id != monthlyRental.RenId)
            {
                return BadRequest();
            }

            _context.Entry(monthlyRental).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MonthlyRentalExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/MonthlyRentals
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<MonthlyRental>> PostMonthlyRental(MonthlyRental monthlyRental)
        {
            _context.MonthlyRentals.Add(monthlyRental);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMonthlyRental", new { id = monthlyRental.RenId }, monthlyRental);
        }

        // DELETE: api/MonthlyRentals/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMonthlyRental(int id)
        {
            var monthlyRental = await _context.MonthlyRentals.FindAsync(id);
            if (monthlyRental == null)
            {
                return NotFound();
            }

            _context.MonthlyRentals.Remove(monthlyRental);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MonthlyRentalExists(int id)
        {
            return _context.MonthlyRentals.Any(e => e.RenId == id);
        }
    }
}
