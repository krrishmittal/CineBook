using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{
    [Route("api/cinemas")]
    [ApiController]
    public class CinemaController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;

        public CinemaController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService;
        }

        // POST api/cinemas/register — Cinema Manager registers cinema
        [HttpPost("register")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> Register([FromBody] RegisterCinemaRequest request)
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _cinemaService.RegisterCinemaAsync(managerId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // GET api/cinemas/my — Cinema Manager sees their cinema
        [HttpGet("my")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> GetMyCinema()
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _cinemaService.GetMyCinemaAsync(managerId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // GET api/cinemas — Admin gets all cinemas
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var result = await _cinemaService.GetAllCinemasAsync(status);
            return Ok(result);
        }

        // PUT api/cinemas/{id}/approve — Admin approves or rejects
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveCinemaRequest request)
        {
            var result = await _cinemaService.ApproveCinemaAsync(id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}