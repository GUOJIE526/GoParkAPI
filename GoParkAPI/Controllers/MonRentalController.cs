using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonRentalController : ControllerBase
    {
        private readonly MonRentalService _monRenService;
        private readonly EasyParkContext _context;
        public MonRentalController(MonRentalService monRentalService, EasyParkContext easyPark)
        {
            _monRenService = monRentalService;
            _context = easyPark;
        }

    }
}
