using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Movies")]
    public class MoviesViewController : Controller
    {
        [HttpGet("{id}")]
        public IActionResult Detail(Guid id) => View("Detail");

        [HttpGet("{id}/Book")]
        public IActionResult Book(Guid id) => View("~/Views/MoviesView/BookTickets.cshtml");
    }


    [Route("api/movies")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        

        // GET api/movies
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? genre,
            [FromQuery] string? status)
        {
            var result = await _movieService.GetAllAsync(search, genre, status);
            return Ok(result);
        }

        // GET api/movies/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _movieService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // POST api/movies
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request)
        {
            var result = await _movieService.CreateAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // PUT api/movies/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMovieRequest request)
        {
            var result = await _movieService.UpdateAsync(id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // DELETE api/movies/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _movieService.DeleteAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }
    }
}