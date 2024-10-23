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
                .Where(res => userCars.Contains(res.Car.LicensePlate)) // 比對車牌號碼
                .Where(res => string.IsNullOrEmpty(licensePlate) || res.Car.LicensePlate == licensePlate) //若有填寫車牌則進一步篩選
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    resTime = res.ResTime,
                    lotName = res.Lot.LotName,
                    licensePlate = res.Car.LicensePlate,
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue,
                    isFinish = res.IsFinish,
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Longitude).FirstOrDefault()

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
                .Where(res => userCars.Contains(res.Car.LicensePlate) && res.Lot.LotName.Contains(lotName))
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    licensePlate = res.Car.LicensePlate,
                    resTime = res.ResTime,
                    lotName = res.Lot.LotName,
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue,
                    isFinish = res.IsFinish,
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Longitude).FirstOrDefault()
                });

            if (reservations == null)
            {
                return null;
            }
            return reservations;
        }

        [HttpGet("GetLotsInfo")]
        public async Task<IActionResult> GetLotsInfo(int lotId)
        {
            var lotInfo = await _context.ParkingLots.Where(lot => lot.LotId == lotId).Select(lot => new LotsInfoDTO
            {
                LotId = lot.LotId,
                LotName = lot.LotName,
                Location = lot.Location,
                SmallCarSpace = lot.SmallCarSpace,
                EtcSpace = lot.EtcSpace,
                MotherSpace = lot.MotherSpace,
                RateRules = lot.RateRules,
                WeekdayRate = lot.WeekdayRate,
                HolidayRate = lot.HolidayRate,
                MonRentalRate = lot.MonRentalRate,
                Tel = lot.Tel,
                ValidSpace = lot.ValidSpace,
                LotImages = _context.ParkingLotImages.Where(img => img.LotId == lotId).Select(img => img.ImgPath).ToList()
            }).FirstOrDefaultAsync();
            if (lotInfo == null)
            {
                return NotFound(new { Message = "無此停車場" });
            }
            return Ok(lotInfo);
        }

        [HttpGet("GetUserCarPlate")]
        public async Task<ActionResult<List<Car>>> GetUserCarPlate(int userId)
        {
            var userCarPlate = await _context.Car.Where(c => c.UserId == userId).Select(c => c.LicensePlate).ToListAsync();
            if (userCarPlate.Count == 0)
            {
                return NotFound(new { Message = "無任何車輛" });
            }
            return Ok(userCarPlate);
        }

        // POST: api/ResService
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("newReservation")]
        public async Task<ActionResult<ReservationDTO>> PostReservation([FromBody] ReservationDTO resDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
            if (userId == null)
            {
                return Unauthorized(new { Message = "無法取得用戶ID" });
            }
            var userCar = await GetUserCars(int.Parse(userId));
            if (!userCar.Contains(resDTO.licensePlate))
            {
                return BadRequest(new {Message = "該車牌不屬於當前用戶"});
            }

            var parkingLot = _context.ParkingLots.FirstOrDefault(lot => lot.LotName == resDTO.lotName);
            if (parkingLot == null)
            {
                return BadRequest(new {Message = "無效的停車場"});
            }

            //創建新的預約
            var newRes = new Reservation
            {
                CarId = _context.Car.FirstOrDefault(car => car.LicensePlate == resDTO.licensePlate).CarId,
                LotId = parkingLot.LotId,
                ResTime = resDTO.resTime,
            };

            _context.Reservation.Add(newRes);
            await _context.SaveChangesAsync();
            return Ok(new {Message = "預約成功", newRes});
        }


        private bool ReservationExists(int id)
        {
            return _context.Reservation.Any(e => e.ResId == id);
        }
    }


}
