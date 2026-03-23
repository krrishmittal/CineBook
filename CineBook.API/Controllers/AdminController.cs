using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("Movies")]
        public IActionResult Movies() => View();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("Dashboard")]
        public IActionResult Dashboard() => View("Movies");

        [HttpGet("Approvals")]
        public IActionResult Approvals() => View();
    }
}