using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineBook.API.Controllers
{
    [Route("api/images")]
    [ApiController]
    [Authorize(Roles = "Admin,CinemaManager")]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(IImageService imageService, ILogger<ImageController> logger)
        {
            _imageService = imageService;
            _logger = logger;
        }

        // POST api/images/upload
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                var url = await _imageService.UploadImageAsync(file);
                return Ok(new { success = true, url });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Image upload validation failed: {Message}", ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed");
                return StatusCode(500, new { success = false, message = "An error occurred while uploading the image." });
            }
        }
    }
}