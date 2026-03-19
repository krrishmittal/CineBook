using CineBook.Application.DTOs.Requests;
using CineBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace CineBook.API.Controllers
{
    // ── View Route ────────────────────────────────────────
    [Route("Checkout")]
    public class CheckoutViewController : Controller
    {
        [HttpGet("Success")]
        public IActionResult Success() => View("~/Views/BookingView/CheckoutSuccess.cshtml");

        [HttpGet("Cancel")]
        public IActionResult Cancel() => View("~/Views/BookingView/CheckoutCancel.cshtml");
    }

    // ── API ───────────────────────────────────────────────
    [Route("api/checkout")]
    [ApiController]
    [Authorize]
    public class CheckOutController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;
        private readonly IBookingService _bookingService;

        public CheckOutController(
            IOptions<StripeSettings> stripeSettings,
            IBookingService bookingService)
        {
            _stripeSettings = stripeSettings.Value;
            _bookingService = bookingService;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // POST api/checkout/create-session
        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession(
            [FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                // Get booking details from DB
                var bookingResult = await _bookingService
                    .GetBookingByIdAsync(request.BookingId, GetUserId());

                if (!bookingResult.Success)
                    return BadRequest(bookingResult);

                var booking = bookingResult.Data;

                // Build line items
                var lineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "inr",
                            UnitAmount = (long)(booking.TotalAmount * 100), // paise
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"🎬 {booking.MovieTitle}",
                                Description = $"{booking.CinemaName} • {booking.HallName} • " +
                                              $"{booking.Seats.Count} Seat(s) • Ref: {booking.BookingReference}",
                                Images = booking.MoviePoster != null
                                    ? new List<string> { booking.MoviePoster }
                                    : null
                            }
                        },
                        Quantity = 1
                    }
                };

                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = $"{baseUrl}/Checkout/Success" +
                                 $"?bookingId={request.BookingId}&session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{baseUrl}/Checkout/Cancel" +
                                $"?bookingId={request.BookingId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "bookingId", request.BookingId.ToString() },
                        { "userId", GetUserId() },
                        { "bookingReference", booking.BookingReference }
                    },
                    CustomerEmail = User.FindFirstValue(ClaimTypes.Email)
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return Ok(new { success = true, sessionId = session.Id, url = session.Url });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST api/checkout/confirm-payment
        // Called after Stripe redirects to success page
        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(
            [FromBody] ConfirmPaymentRequest request)
        {
            try
            {
                StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

                // Verify session with Stripe
                var service = new SessionService();
                var session = await service.GetAsync(request.SessionId);

                if (session.PaymentStatus != "paid")
                    return BadRequest(new { success = false, message = "Payment not completed" });

                // Confirm booking
                var result = await _bookingService.ConfirmBookingAsync(
                    GetUserId(),
                    new ConfirmBookingRequest { BookingId = request.BookingId });

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (StripeException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}