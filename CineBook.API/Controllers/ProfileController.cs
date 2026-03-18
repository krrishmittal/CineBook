using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{
    [Route("Profile")]
    public class ProfileViewController : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View();
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _profileService.GetProfileAsync(GetUserId());
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request)
        {
            var result = await _profileService.UpdateProfileAsync(
                GetUserId(), request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // PUT api/profile/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request)
        {
            var result = await _profileService.ChangePasswordAsync(
                GetUserId(), request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
