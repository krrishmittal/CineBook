using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}