using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using GoParkAPI.DTO;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Cars_Controller : ControllerBase
    {
        private readonly EasyParkContext _context;

        public Cars_Controller(EasyParkContext context)
        {
            _context = context;
        }

        // GET: api/Cars_
        [HttpGet]
        public async Task<IEnumerable<CarsDTO>> GetCars(int userId)
        {
            var cars =  _context.Cars
                .Where(car => car.UserId == userId)
                .Select(car => new CarsDTO
                {
                    carId = car.CarId,
                    licensePlate = car.LicensePlate,
                    registerDate = car.RegisterDate,
                    isActive = car.IsActive
                });
            if (cars == null)
            {
                return null;
            }
            return cars;
        }


        // PUT: api/Cars_/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut()]
        public async Task<string> PutCars(List<CarsDTO> carsDto)
        {
            foreach (var car in carsDto)
            {
                // 找到對應的車輛
                var updateCar = await _context.Cars.FindAsync(car.carId);
                if (updateCar == null)
                {
                    return "修改失敗";
                }
                updateCar.IsActive = car.isActive;
                _context.Entry(updateCar).Property(c => c.IsActive).IsModified = true;
                
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return "修改成功";
        }

        // POST: api/Cars_
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<string> PostCar(int userId, List<CarsDTO> carsDto)
        {
            foreach (var car in carsDto)
            {
                Car newCar = new Car
                {
                    UserId = userId,
                    LicensePlate = car.licensePlate,
                    RegisterDate = DateTime.Now,
                    IsActive = car.isActive

                };
                _context.Cars.Add(newCar);
            };
            

            try
            {
                // 儲存所有新增的車牌資料
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return $"新增失敗: {ex.Message}";
            }

            return "新增成功";
        }

        // DELETE: api/Cars_/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCar(int id)
        //{
        //    var car = await _context.Car.FindAsync(id);
        //    if (car == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Car.Remove(car);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.CarId == id);
        }
    }
}
