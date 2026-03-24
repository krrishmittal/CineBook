using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        
        /// <summary>
        /// Handles HTTP GET requests for the Movies view.
        /// </summary>
        /// <returns>A view result that renders the Movies page.</returns>
        [HttpGet("Movies")]
        public IActionResult Movies() => View();


        /// <summary>
        /// Handles HTTP GET requests for the Dashboard and returns the Movies view.    
        /// </summary>
        /// <returns>A view result that renders the Movies view.</returns>
        [HttpGet("Dashboard")]
        public IActionResult Dashboard() => View("Movies");


        /// <summary>
        /// Handles HTTP GET requests for the Approvals view.   
        /// </summary>
        /// <returns>A view result that renders the Approvals view to the client.</returns>
        [HttpGet("Approvals")]
        public IActionResult Approvals() => View();
    }
}