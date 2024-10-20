using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Azure.Core;
using GoParkAPI.DTO;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public ReservationsController(EasyParkContext context)
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

        //載入用戶的預訂資料 & 透過車牌篩選(Option)
        // GET: api/Reservations
        [HttpGet]
        public async Task<IEnumerable<ReservationDTO>> GetReservation(int userId, string? licensePlate)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);

            //篩選該用戶車牌的預訂資料
            var reservations = _context.Reservation
                .Where(res => userCars.Contains(res.LicensePlate)) // 比對車牌號碼
                .Where(res => string.IsNullOrEmpty(licensePlate) || res.LicensePlate == licensePlate) //若有填寫車牌則進一步篩選
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    resTime = res.ResTime,
                    lotName = res.LotName,
                    licensePlate = res.LicensePlate,
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue,
                    isFinish = res.IsFinish,
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.LotName).Select(lot => lot.Longitude).FirstOrDefault()

                });
            if (reservations == null)
            {
                return null;
            }
            return reservations;
        }

        //搜尋"停車場"載入預定紀錄
        [HttpGet("search/{lotName}")]
        public async Task<IEnumerable<ReservationDTO>> SearchReservationsByLotname(int userId, string lotName)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);

            // 根據停車場名稱模糊查詢訂單
            var reservations = _context.Reservation
                .Where(res => userCars.Contains(res.LicensePlate) && res.LotName.Contains(lotName))
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    licensePlate = res.LicensePlate,
                    resTime = res.ResTime,
                    lotName = res.LotName,
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue,
                    isFinish = res.IsFinish,
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.LotName).Select(lot => lot.Longitude).FirstOrDefault()
                });

            if (reservations == null)
            {
                return null;
            }
            return reservations;
        }

        //依照日期篩選(ex.過去30天)
        //依照是否完成訂單篩選


        // GET: api/Reservations/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Reservation>> GetReservation(int id)
        //{
        //    var reservation = await _context.Reservation.FindAsync(id);

        //    if (reservation == null)
        //    {
        //        return NotFound();
        //    }

        //    return reservation;
        //}

        // PUT: api/Reservations/5    應該不需用到?
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutReservation(int id, Reservation reservation)
        //{
        //    if (id != reservation.ResId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(reservation).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!ReservationExists(id))
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

        // POST: api/Reservations  應該不需用到?
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        //public async Task<ActionResult<Reservation>> PostReservation(Reservation reservation)
        //{
        //    _context.Reservation.Add(reservation);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetReservation", new { id = reservation.ResId }, reservation);
        //}

        // DELETE: api/Reservations/5  不會用到(不能刪)
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteReservation(int id)
        //{
        //    var reservation = await _context.Reservation.FindAsync(id);
        //    if (reservation == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Reservation.Remove(reservation);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool ReservationExists(int id)
        {
            return _context.Reservation.Any(e => e.ResId == id);
        }
    }


}
