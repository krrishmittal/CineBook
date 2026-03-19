using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{

    [Route("Favourites")]
    public class FavouriteViewController : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View("~/Views/FavouritesView/Index.cshtml");
    }

    [Route("api/favourites")]
    [ApiController]
    [Authorize]
    public class FavouriteController : ControllerBase
    {
        private readonly IFavouriteService _favouriteService;

        public FavouriteController(IFavouriteService favouriteService)
        {
            _favouriteService = favouriteService;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // POST api/favourites/toggle/{movieId}
        [HttpPost("toggle/{movieId}")]
        public async Task<IActionResult> Toggle(Guid movieId)
        {
            var result = await _favouriteService.ToggleFavouriteAsync(GetUserId(), movieId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // GET api/favourites/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var result = await _favouriteService.GetMyFavouritesAsync(GetUserId());
            return Ok(result);
        }

        // GET api/favourites/ids
        [HttpGet("ids")]
        public async Task<IActionResult> GetIds()
        {
            var result = await _favouriteService.GetFavouriteIdsAsync(GetUserId());
            return Ok(result);
        }
    }
}