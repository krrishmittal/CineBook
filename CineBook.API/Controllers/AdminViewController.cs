using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Admin")]
    public class AdminViewController : Controller
    {
        [HttpGet("Movies")]
        public IActionResult Movies() => View();

        [HttpGet("Dashboard")]
        public IActionResult Dashboard() => View("Movies");
    }
}