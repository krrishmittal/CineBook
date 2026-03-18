using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Movies")]
    public class MoviesViewController : Controller
    {
        [HttpGet("{id}")]
        public IActionResult Detail(Guid id) => View("Detail");
    }
}
