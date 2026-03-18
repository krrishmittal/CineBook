using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineBook.API.Controllers
{

    [Route("Booking")]
    public class BookingViewController : Controller
    {
        [HttpGet("Seats")]
        public IActionResult Seats() => View("~/Views/BookingView/Seats.cshtml");

        [HttpGet("MyBookings")]
        public IActionResult MyBookings() => View("~/Views/BookingView/MyBookings.cshtml");
    }

    [Route("api/bookings")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET api/bookings/seat-layout/{showtimeId}
        [HttpGet("seat-layout/{showtimeId}")]
        public async Task<IActionResult> GetSeatLayout(Guid showtimeId)
        {
            var result = await _bookingService.GetSeatLayoutAsync(showtimeId, GetUserId());
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // POST api/bookings/initiate
        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] InitiateBookingRequest request)
        {
            var result = await _bookingService.InitiateBookingAsync(GetUserId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // POST api/bookings/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmBookingRequest request)
        {
            var result = await _bookingService.ConfirmBookingAsync(GetUserId(), request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // GET api/bookings/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings()
        {
            var result = await _bookingService.GetMyBookingsAsync(GetUserId());
            return Ok(result);
        }

        // DELETE api/bookings/{id}/cancel
        [HttpDelete("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var result = await _bookingService.CancelBookingAsync(id, GetUserId());
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}