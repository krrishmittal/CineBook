using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Manager")]
    public class ManagerViewController : Controller
    {
        [HttpGet("Dashboard")]
        public IActionResult Dashboard() => RedirectToAction("Cinema");

        [HttpGet("Cinema")]
        public IActionResult Cinema() => View();

        [HttpGet("Halls")]
        public IActionResult Halls() => View();

        [HttpGet("Showtimes")]
        public IActionResult Showtimes() => View();
    }
}
