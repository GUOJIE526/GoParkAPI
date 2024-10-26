﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GoParkAPI.DTO;
using GoParkAPI.Models;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public SurveyController(EasyParkContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostSurvey(SurveyDTO survey)
        {
            if (ModelState.IsValid)
            {
                Survey newsurvey = new Survey {
                    Id = 0,
                    UserId = survey.UserId,
                    Question = survey.Question,
                    IsReplied = false,
                    SubmittedAt = DateTime.Now,
                    Status = "未回覆"
                };

                _context.Surveys.Add(newsurvey);
                await _context.SaveChangesAsync();

                return new JsonResult(new { status = "success" });
            }
            else
            {
                return new JsonResult(new { status = "fail" });
            }

            
        }

    }
}
