using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using GoParkAPI.DTO;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntryExitManagementsController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public EntryExitManagementsController(EasyParkContext context)
        {
            _context = context;
        }

        //先抓出該用戶註冊的車牌
        private async Task<List<string>> GetUserCars(int userId)
        {
            return await _context.Car
                .Where(car => car.UserId == userId)
                .Select(car => car.LicensePlate)
                .ToListAsync();
        }

        //載入用戶的停車紀錄 & 透過車牌篩選(Option)
        // GET: api/EntryExitManagements
        [HttpGet]
        public async Task<IEnumerable<EntryExitManagementDTO>> GetEntryExit(int userId, string? licensePlate)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);

            //篩選該用戶車牌的預訂資料
            var parkingRecords = _context.EntryExitManagement
                .Where(record => userCars.Contains(record.Car.LicensePlate)) // 比對車牌號碼
                .Where(record => string.IsNullOrEmpty(licensePlate) || record.Car.LicensePlate == licensePlate) //若有填寫車牌則進一步篩選
                .Where(record => record.Parktype == "reservation")  //只顯示預定的停車紀錄，月租不顯示
                .Select(record => new EntryExitManagementDTO
                {
                    
                    entryexitId = record.EntryexitId,
                    lotName = record.Lot.LotName,
                    licensePlate = record.Car.LicensePlate,
                    entryTime = (DateTime)record.EntryTime,
                    exitTime = record.ExitTime,
                    //totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
                    amount = record.Amount
                });
            if (parkingRecords == null)
            {
                return null;
            }
            return parkingRecords;
        }

        //搜尋"停車場"載入停車紀錄
        [HttpGet("search/{lotName}")]
        public async Task<IEnumerable<EntryExitManagementDTO>> SearchEntryExitByLotname(int userId, string lotName)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);

            // 根據停車場名稱模糊查詢停車紀錄
            var parkingRecords = _context.EntryExitManagement
                .Where(record => userCars.Contains(record.Car.LicensePlate) && record.Lot.LotName.Contains(lotName))
                .Select(record => new EntryExitManagementDTO
                {
                    entryexitId = record.EntryexitId,
                    lotName = record.Lot.LotName,
                    licensePlate = record.Car.LicensePlate,
                    entryTime = (DateTime)record.EntryTime,
                    exitTime = record.ExitTime,
                    //totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
                    amount = record.Amount
                });

            if (parkingRecords == null)
            {
                return null;
            }
            return parkingRecords;
        }



        // GET: api/EntryExitManagements/5
        [HttpGet("{id}")]
        public async Task<ParkingDetailDTO> GetEntryExitDetail(int id)
        {
            var parkingDetail = await _context.EntryExitManagement
                .Where(record => record.EntryexitId == id)
                .Select(record => new ParkingDetailDTO
                {
                    entryexitId = record.EntryexitId,
                    lotName = record.Lot.LotName,
                    district = record.Lot.District,
                    location = record.Lot.Location,
                    latitude = record.Lot.Latitude,
                    longitude = record.Lot.Longitude,
                    licensePlate = record.Car.LicensePlate,
                    entryTime = (DateTime)record.EntryTime,
                    exitTime = record.ExitTime,
                    //totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
                    amount = record.Amount
                })
                .FirstOrDefaultAsync(); ;

            if (parkingDetail == null)
            {
                return null;
            }
            return parkingDetail;

            //var record = await _context.EntryExitManagement.FindAsync(id);
            //if(record != null)
            //{
            //    EntryExitManagementDTO parkingDetail = new EntryExitManagementDTO
            //    {
            //        entryexitId = record.EntryexitId,
            //        lotName = record.Lot.LotName,
            //        district = record.Lot.District,
            //        location = record.Lot.Location,
            //        licensePlate = record.Car.LicensePlate,
            //        entryTime = record.EntryTime,
            //        exitTime = record.ExitTime,
            //        //totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
            //        amount = record.Amount
            //    };
            //    return parkingDetail;
            //}
            //return null;


        }

        // PUT: api/EntryExitManagements/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutEntryExitManagement(int id, EntryExitManagement entryExitManagement)
        //{
        //    if (id != entryExitManagement.EntryexitId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(entryExitManagement).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!EntryExitManagementExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/EntryExitManagements
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<EntryExitManagement>> PostEntryExitManagement(EntryExitManagement entryExitManagement)
        //{
        //    _context.EntryExitManagement.Add(entryExitManagement);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetEntryExitManagement", new { id = entryExitManagement.EntryexitId }, entryExitManagement);
        //}

        // DELETE: api/EntryExitManagements/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteEntryExitManagement(int id)
        //{
        //    var entryExitManagement = await _context.EntryExitManagement.FindAsync(id);
        //    if (entryExitManagement == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.EntryExitManagement.Remove(entryExitManagement);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool EntryExitManagementExists(int id)
        {
            return _context.EntryExitManagement.Any(e => e.EntryexitId == id);
        }
    }
}
