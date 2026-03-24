using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{
    [Route("api/showtimes")]
    [ApiController]
    public class ShowtimeController : ControllerBase
    {
        private readonly IShowtimeService _showtimeService;

        public ShowtimeController(IShowtimeService showtimeService)
        {
            _showtimeService = showtimeService;
        }

        private string GetManagerId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // POST api/showtimes
        [HttpPost]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request)
        {
            var result = await _showtimeService.CreateShowtimeAsync(GetManagerId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // GET api/showtimes/my  (paged)
        // ?date=2025-01-15  &page=1  &pageSize=10
        [HttpGet("my")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> GetMyShowtimes(
            [FromQuery] string? date,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _showtimeService.GetMyShowtimesPagedAsync(
                GetManagerId(), date, page, pageSize);

            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // PUT api/showtimes/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShowtimeRequest request)
        {
            var result = await _showtimeService.UpdateShowtimeAsync(id, GetManagerId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // DELETE api/showtimes/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _showtimeService.DeleteShowtimeAsync(id, GetManagerId());
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // GET api/showtimes/movie/{movieId} — public
        [HttpGet("movie/{movieId}")]
        public async Task<IActionResult> GetByMovie(Guid movieId, [FromQuery] string? date)
        {
            var result = await _showtimeService.GetShowtimesByMovieAsync(movieId, date);
            return Ok(result);
        }
    }
}