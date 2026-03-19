using CineBook.API.Hubs;
using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using CineBook.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CineBook.API.Controllers
{

    [Route("Booking")]
    [Route("Bookings")]  
    public class BookingViewController : Controller
    {
        [HttpGet("Seats")]
        public IActionResult Seats() => View("~/Views/BookingView/Seats.cshtml");

        [HttpGet("MyBookings")]
        public IActionResult MyBookings() => View("~/Views/BookingView/MyBookings.cshtml");

        [HttpGet("")] 
        public IActionResult Index() => View("~/Views/BookingView/MyBookings.cshtml");

        [HttpGet("Refunds")]
        public IActionResult Refunds() => View("~/Views/BookingView/Refunds.cshtml");

        [HttpGet("CinemaBookings")]
        public IActionResult CinemaBookings() => View("~/Views/Manager/CinemaBookings.cshtml");
    }

    [Route("api/bookings")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ITicketService _ticketService;
        private readonly IHubContext<SeatHub> _seatHub;

        public BookingController(IBookingService bookingService, ITicketService ticketService, IHubContext<SeatHub>seatHub)
        {
            _bookingService = bookingService;
            _ticketService = ticketService;
            _seatHub = seatHub;
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
             
            await _seatHub.Clients
                .Group($"showtime-{request.ShowtimeId}")
                .SendAsync("SeatsLocked", new
                {
                    seatIds = request.SeatIds,
                    lockedByUserId = GetUserId()
                });

            return Ok(result);
        }

        // POST api/bookings/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmBookingRequest request)
        {
            var result = await _bookingService.ConfirmBookingAsync(GetUserId(), request);
            if (!result.Success) return BadRequest(result);

            // ✅ Get seat info from booking seats response
            await _seatHub.Clients
                .Group($"showtime-{result.Data.ShowtimeId}")
                .SendAsync("SeatsBooked", new
                {
                    seatIds = result.Data.Seats
                        .Select(s => new { row = s.Row, seatNumber = s.SeatNumber })
                        .ToList()
                });

            return Ok(result);
        }

        [HttpPost("{id}/release")]
        public async Task<IActionResult> ReleaseSeats(Guid id)
        {
            var result = await _bookingService.ReleaseLockedSeatsAsync(id, GetUserId());
            if (!result.Success) return BadRequest(result);

            // ── Broadcast seat release to ALL users in this showtime ──
            // Get the booking's showtimeId first
            var booking = await _bookingService.GetBookingByIdAsync(id, GetUserId());
            if (booking.Success)
            {
                await _seatHub.Clients
                    .Group($"showtime-{booking.Data.ShowtimeId}")
                    .SendAsync("SeatsReleased", new
                    {
                        bookingId = id
                    });
            }

            return Ok(result);
        }

        // GET api/bookings/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings()
        {
            var result = await _bookingService.GetMyBookingsAsync(GetUserId());
            return Ok(result);
        }

        // GET api/bookings/cinema — Cinema Manager sees all bookings for their cinema
        [HttpGet("cinema")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> GetCinemaBookings([FromQuery] string? date)
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _bookingService.GetCinemaBookingsAsync(managerId, date);
            return Ok(result);
        }

        // GET api/bookings/{id}/ticket
        [HttpGet("{id}/ticket")]
        public async Task<IActionResult> DownloadTicket(Guid id)
        {
            try
            {
                var pdfBytes = await _ticketService.GenerateTicketPdfAsync(id, GetUserId());
                return File(pdfBytes, "application/pdf", $"CineBook_Ticket_{id}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET api/bookings/cinema/cancelled
        [HttpGet("cinema/cancelled")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> GetCancelledBookings()
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _bookingService.GetCancelledBookingsAsync(managerId);
            return Ok(result);
        }

        // PUT api/bookings/{id}/refund
        [HttpPut("{id}/refund")]
        [Authorize(Roles = "CinemaManager")]
        public async Task<IActionResult> ProcessRefund(Guid id, [FromBody] ProcessRefundRequest request)
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _bookingService.ProcessRefundAsync(id, managerId, request?.Note);
            if (!result.Success) return BadRequest(result);
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