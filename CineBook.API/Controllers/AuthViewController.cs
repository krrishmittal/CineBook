using CineBook.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("Auth")]
    public class AuthViewController : Controller
    {
        // GET /Auth/Login
        [HttpGet("Login")]
        public IActionResult Login() => View();

        // GET /Auth/Register
        [HttpGet("Register")]
        public IActionResult Register() => View();

        //GET /Auth/ForgotPassword
       [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword() => View();

    }
}
