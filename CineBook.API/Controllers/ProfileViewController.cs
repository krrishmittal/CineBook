using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Profile")]
    public class ProfileViewController : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View();
    }
}