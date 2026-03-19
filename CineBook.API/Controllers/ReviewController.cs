using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private string? GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET api/reviews/movie/{movieId}
        [HttpGet("movie/{movieId}")]
        public async Task<IActionResult> GetMovieReviews(Guid movieId)
        {
            var result = await _reviewService.GetMovieReviewsAsync(movieId, GetUserId());
            return Ok(result);
        }

        // POST api/reviews
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
        {
            var result = await _reviewService.CreateReviewAsync(GetUserId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // DELETE api/reviews/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _reviewService.DeleteReviewAsync(id, GetUserId());
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}