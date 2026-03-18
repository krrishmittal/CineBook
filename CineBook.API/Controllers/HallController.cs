using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{
    [Route("api/halls")]
    [ApiController]
    [Authorize(Roles = "CinemaManager")]
    public class HallController : ControllerBase
    {
        private readonly IHallService _hallService;

        public HallController(IHallService hallService)
        {
            _hallService = hallService;
        }

        private string GetManagerId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // POST api/halls
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHallRequest request)
        {
            var result = await _hallService.CreateHallAsync(GetManagerId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // GET api/halls
        [HttpGet]
        public async Task<IActionResult> GetMyHalls()
        {
            var result = await _hallService.GetMyHallsAsync(GetManagerId());
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // GET api/halls/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _hallService.GetHallByIdAsync(id, GetManagerId());
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // PUT api/halls/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHallRequest request)
        {
            var result = await _hallService.UpdateHallAsync(id, GetManagerId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // DELETE api/halls/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _hallService.DeleteHallAsync(id, GetManagerId());
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }
    }
}