using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
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


    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }


        [HttpPost("register/user")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request, "User");
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("register/cinema-manager")]
        public async Task<IActionResult> RegisterCinemaManager(
            [FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request, "CinemaManager");
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
                return Unauthorized(result);
            return Ok(result);
        }

        // POST api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // POST api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }


    }
}