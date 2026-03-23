using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Domain.Enums;
using CineBook.Infrastructure.Persistence;
using CineBook.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly IConfiguration _config;
        private readonly ISmsService _smsService;
        private readonly IImageService _imageService;
        private readonly ITicketService _ticketService;
        private readonly IServiceScopeFactory _scopeFactory;
        private const decimal ConvenienceFeeRate = 0.02m;

        public BookingService(
            AppDbContext context,
            ILogger<BookingService> logger,
            ISmsService smsService,
            ITicketService ticketService,
            IImageService imageService,
            IConfiguration config,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _logger = logger;
            _smsService = smsService;
            _ticketService = ticketService;
            _imageService = imageService;
            _config = config;
            _scopeFactory = scopeFactory;
        }

        // ── Get Seat Layout ───────────────────────────────────
        public async Task<ApiResponse<SeatLayoutResponse>> GetSeatLayoutAsync(
            Guid showtimeId, string userId)
        {
            var showtime = await _context.Showtimes
                .IgnoreQueryFilters()
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Include(s => s.ShowtimeSeats)
                    .ThenInclude(ss => ss.Seat)
                .FirstOrDefaultAsync(s => s.Id == showtimeId && s.IsActive);

            if (showtime == null)
            {
                _logger.LogWarning("GetSeatLayoutAsync failed: Showtime {ShowtimeId} not found or inactive", showtimeId);
                return ApiResponse<SeatLayoutResponse>.Fail("Showtime not found", 404, "GetSeatLayoutAsync");
            }

            var expiredLocks = showtime.ShowtimeSeats
                .Where(ss => ss.Status == SeatStatus.Locket
                    && ss.LockedAt.HasValue
                    && ss.LockedAt.Value.AddMinutes(10) < DateTime.UtcNow)
                .ToList();

            if (expiredLocks.Any())
            {
                _logger.LogInformation("Releasing {Count} expired seat locks for Showtime {ShowtimeId}", expiredLocks.Count, showtimeId);
                foreach (var lock_ in expiredLocks)
                {
                    lock_.Status = SeatStatus.Available;
                    lock_.LockedByUserId = null;
                    lock_.LockedAt = null;
                }
                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException)
                {
                    _logger.LogWarning("Concurrency exception handled gracefully while releasing locks for {ShowtimeId}", showtimeId);
                }
            }

            var seats = showtime.ShowtimeSeats
                .OrderBy(ss => ss.Seat.Row)
                .ThenBy(ss => ss.Seat.SeatNumber)
                .Select(ss => new SeatInfo
                {
                    SeatId = ss.SeatId,
                    ShowtimeSeatId = ss.Id,
                    Row = ss.Seat.Row,
                    SeatNumber = ss.Seat.SeatNumber,
                    SeatType = ss.Seat.SeatType,
                    SeatTypeLabel = ss.Seat.SeatType.ToString(),
                    Status = ss.Status == SeatStatus.Available ? "Available"
                           : ss.Status == SeatStatus.Locket ? "Locked"
                           : "Booked",
                    IsMyLock = ss.LockedByUserId == userId,
                    Price = ss.Seat.SeatType == SeatType.Standard ? showtime.PriceStandard
                          : ss.Seat.SeatType == SeatType.Premium ? showtime.PricePremium
                          : showtime.PriceVIP
                }).ToList();

            _logger.LogInformation("Seat layout fetched for Showtime {ShowtimeId} by User {UserId}", showtimeId, userId);

            return ApiResponse<SeatLayoutResponse>.Ok(new SeatLayoutResponse
            {
                ShowtimeId = showtime.Id,
                MovieTitle = showtime.Movie.Title,
                CinemaName = showtime.Cinema.CinemaName,
                HallName = showtime.Hall.HallName,
                HallType = showtime.Hall.HallType.ToString(),
                StartTime = TimeZoneHelper.ConvertToIST(showtime.StartTime),
                PriceStandard = showtime.PriceStandard,
                PricePremium = showtime.PricePremium,
                PriceVIP = showtime.PriceVIP,
                Seats = seats
            }, "Seat layout fetched");
        }

        // ── Initiate Booking ──────────────────────────────────
        public async Task<ApiResponse<BookingResponse>> InitiateBookingAsync(
            string userId, InitiateBookingRequest request)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Include(s => s.ShowtimeSeats).ThenInclude(ss => ss.Seat)
                .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId && s.IsActive);

            if (showtime == null)
                return ApiResponse<BookingResponse>.Fail("Showtime not found", 404, "Showtime");

            if (!request.SeatIds.Any())
                return ApiResponse<BookingResponse>.Fail("Select at least one seat", 400, "Seats");

            if (request.SeatIds.Count > 10)
                return ApiResponse<BookingResponse>.Fail("Maximum 10 seats per booking", 400, "Seats");

            var showtimeSeats = showtime.ShowtimeSeats
                .Where(ss => request.SeatIds.Contains(ss.SeatId)).ToList();

            if (showtimeSeats.Count != request.SeatIds.Count)
                return ApiResponse<BookingResponse>.Fail("One or more seats not found", 400, "Seats");

            var unavailable = showtimeSeats
                .Where(ss => ss.Status != SeatStatus.Available
                    && !(ss.Status == SeatStatus.Locket && ss.LockedByUserId == userId))
                .ToList();

            if (unavailable.Any())
                return ApiResponse<BookingResponse>.Fail("One or more seats are already taken", 400, "Seats");

            var existingBooking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.UserId == userId
                    && b.ShowtimeId == request.ShowtimeId
                    && b.Status == BookingStatus.Pending);

            if (existingBooking != null)
            {
                var oldSeatIds = existingBooking.BookingSeats.Select(bs => bs.SeatId).ToList();
                var oldLocks = showtime.ShowtimeSeats.Where(ss => oldSeatIds.Contains(ss.SeatId)).ToList();
                foreach (var l in oldLocks) { l.Status = SeatStatus.Available; l.LockedByUserId = null; l.LockedAt = null; }
                _context.Bookings.Remove(existingBooking);
            }

            foreach (var ss in showtimeSeats) { ss.Status = SeatStatus.Locket; ss.LockedByUserId = userId; ss.LockedAt = DateTime.UtcNow; }

            decimal subTotal = showtimeSeats.Sum(ss =>
                ss.Seat.SeatType == SeatType.Standard ? showtime.PriceStandard
                : ss.Seat.SeatType == SeatType.Premium ? showtime.PricePremium
                : showtime.PriceVIP);

            decimal convenienceFee = Math.Round(subTotal * ConvenienceFeeRate, 2);
            decimal totalAmount = subTotal + convenienceFee;

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingReference = GenerateReference(),
                UserId = userId,
                ShowtimeId = request.ShowtimeId,
                SubTotal = subTotal,
                ConvenienceFee = convenienceFee,
                TotalAmount = totalAmount,
                Status = BookingStatus.Pending,
                BookedAt = DateTime.UtcNow
            };

            var bookingSeats = showtimeSeats.Select(ss => new BookingSeat
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                SeatId = ss.SeatId,
                SeatType = ss.Seat.SeatType,
                PricePaid = ss.Seat.SeatType == SeatType.Standard ? showtime.PriceStandard
                          : ss.Seat.SeatType == SeatType.Premium ? showtime.PricePremium
                          : showtime.PriceVIP
            }).ToList();

            await _context.Bookings.AddAsync(booking);
            await _context.BookingSeats.AddRangeAsync(bookingSeats);
            await _context.SaveChangesAsync();

            await _context.Payments.AddAsync(new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                StripePaymentIntentId = "",
                Amount = totalAmount,
                Status = PaymentStatus.Pending,
                PaidAt = null
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} (Ref: {Ref}) initiated by User {UserId} with {Count} seats",
                booking.Id, booking.BookingReference, userId, bookingSeats.Count);

            booking.BookingSeats = bookingSeats;
            return ApiResponse<BookingResponse>.Ok(MapToResponse(booking, showtime, bookingSeats), "Seats locked! Proceed to payment.");
        }

        // ── Confirm Booking ───────────────────────────────────
        public async Task<ApiResponse<BookingResponse>> ConfirmBookingAsync(
            string userId, ConfirmBookingRequest request)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId
                    && b.UserId == userId
                    && b.Status == BookingStatus.Pending);

            if (booking == null)
                return ApiResponse<BookingResponse>.Fail("Booking not found or already processed", 404, "Booking");

            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(ss => ss.ShowtimeId == booking.ShowtimeId && seatIds.Contains(ss.SeatId))
                .ToListAsync();

            var snatchedSeats = showtimeSeats.Where(ss =>
                ss.Status == SeatStatus.Booked ||
                (ss.Status == SeatStatus.Locket && ss.LockedByUserId != userId)).ToList();

            if (snatchedSeats.Any())
            {
                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                return ApiResponse<BookingResponse>.Fail(
                    "Your seat lock expired during checkout and one or more seats were claimed. Your order was cancelled, and any payment will be refunded.", 400, "Seats");
            }

            foreach (var ss in showtimeSeats) { ss.Status = SeatStatus.Booked; ss.LockedByUserId = null; ss.LockedAt = null; }

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} confirmed", booking.Id);

            // ── Fire background task with NEW scope ───────────
            var bookingIdCopy = booking.Id;
            var userIdCopy = userId;
            var bookingRefCopy = booking.BookingReference;
            var scopeFactory = _scopeFactory;

            _ = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var ticketSvc = scope.ServiceProvider.GetRequiredService<ITicketService>();
                var imageSvc = scope.ServiceProvider.GetRequiredService<IImageService>();
                var smsSvc = scope.ServiceProvider.GetRequiredService<ISmsService>();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var log = scope.ServiceProvider.GetRequiredService<ILogger<BookingService>>();

                try
                {
                    log.LogInformation("🎬 Background: Generating PDF for Booking {BookingId}", bookingIdCopy);

                    // Step 1 — Generate PDF bytes
                    var pdfBytes = await ticketSvc.GenerateTicketPdfAsync(bookingIdCopy, userIdCopy);
                    log.LogInformation("✅ PDF generated ({Size} bytes) for Booking {BookingId}", pdfBytes.Length, bookingIdCopy);

                    // Step 2 — Convert PDF to image + upload to Cloudinary
                    string? ticketImageUrl = null;
                    try
                    {
                        var imageFileName = $"CineBook_Ticket_{bookingRefCopy}";
                        ticketImageUrl = await imageSvc.UploadPdfAsImageAsync(pdfBytes, imageFileName);
                        log.LogInformation("✅ Ticket image uploaded to Cloudinary: {Url}", ticketImageUrl);

                        // Step 3 — Save image URL to DB
                        var b = await ctx.Bookings.FindAsync(bookingIdCopy);
                        if (b != null)
                        {
                            b.TicketPdfUrl = ticketImageUrl;
                            await ctx.SaveChangesAsync();
                            log.LogInformation("✅ TicketImageUrl saved to DB for Booking {BookingId}", bookingIdCopy);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "⚠️ PDF to image conversion failed — sending WhatsApp without image");
                    }

                    // Step 4 — Send WhatsApp (with image if available)
                    await SendWhatsAppConfirmation(bookingIdCopy, ticketImageUrl, ctx, smsSvc, log);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "❌ Background task failed for Booking {BookingId}", bookingIdCopy);
                }
            });

            return ApiResponse<BookingResponse>.Ok(
                MapToResponse(booking, booking.Showtime, booking.BookingSeats.ToList()),
                "Booking confirmed!");
        }

        // ── Send WhatsApp with optional image ────────────────
        private static async Task SendWhatsAppConfirmation(
            Guid bookingId,
            string? ticketImageUrl,
            AppDbContext context,
            ISmsService smsService,
            ILogger log)
        {
            try
            {
                var booking = await context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                    .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null) { log.LogWarning("SendWhatsAppConfirmation: Booking {Id} not found", bookingId); return; }

                var phone = booking.User?.PhoneNumber;
                if (string.IsNullOrEmpty(phone)) { log.LogWarning("SendWhatsAppConfirmation: No phone for User {UserId}", booking.UserId); return; }

                var start = TimeZoneHelper.ConvertToIST(booking.Showtime.StartTime);
                var dateStr = start.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                var timeStr = start.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                var seats = string.Join(", ", booking.BookingSeats
                    .OrderBy(s => s.Seat.Row).ThenBy(s => s.Seat.SeatNumber)
                    .Select(s => $"{s.Seat.Row}{s.Seat.SeatNumber}"));

                var message =
                    $"🎬 *Booking Confirmed!*\n\n" +
                    $"Hi {booking.User.FullName}! Your tickets are confirmed.\n\n" +
                    $"*{booking.Showtime.Movie.Title}*\n" +
                    $"━━━━━━━━━━━━━━━━━━━━\n" +
                    $"📍 *Cinema:* {booking.Showtime.Cinema.CinemaName}\n" +
                    $"🏟 *Hall:* {booking.Showtime.Hall.HallName}\n" +
                    $"📅 *Date:* {dateStr}\n" +
                    $"⏰ *Time:* {timeStr}\n" +
                    $"🪑 *Seats:* {seats}\n" +
                    $"━━━━━━━━━━━━━━━━━━━━\n" +
                    $"🎟 *Ref:* {booking.BookingReference}\n" +
                    $"💰 *Paid:* ₹{booking.TotalAmount}\n" +
                    $"━━━━━━━━━━━━━━━━━━━━\n" +
                    (ticketImageUrl != null
                        ? $"🎫 Your ticket is attached above 👆\n\n"
                        : "") +
                    $"Please arrive 15 minutes early. Enjoy the show! 🍿";

                // ── Send with image or plain text ─────────────
                if (!string.IsNullOrEmpty(ticketImageUrl))
                    await smsService.SendWhatsAppWithMediaAsync(phone, message, ticketImageUrl);
                else
                    await smsService.SendWhatsAppAsync(phone, message);

                log.LogInformation("✅ WhatsApp confirmation sent to {Phone}", phone);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "❌ WhatsApp send failed for Booking {BookingId}", bookingId);
            }
        }

        // ── Get My Bookings ───────────────────────────────────
        public async Task<ApiResponse<List<BookingResponse>>> GetMyBookingsAsync(string userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} bookings for User {UserId}", bookings.Count, userId);
            return ApiResponse<List<BookingResponse>>.Ok(
                bookings.Select(b => MapToResponse(b, b.Showtime, b.BookingSeats.ToList())).ToList(), "Bookings fetched");
        }

        // ── Get Cinema Bookings ───────────────────────────────
        public async Task<ApiResponse<List<BookingResponse>>> GetCinemaBookingsAsync(string managerId, string? date)
        {
            var cinema = await _context.Cinemas.FirstOrDefaultAsync(c => c.ManagerUserId == managerId);
            if (cinema == null) return ApiResponse<List<BookingResponse>>.Fail("Cinema not found", 404, "Cinema");

            var query = _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .Include(b => b.User)
                .Where(b => b.Showtime.CinemaId == cinema.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
                query = query.Where(b => b.Showtime.StartTime.Date == filterDate.Date);

            var bookings = await query.OrderByDescending(b => b.BookedAt).ToListAsync();
            return ApiResponse<List<BookingResponse>>.Ok(
                bookings.Select(b => MapToResponse(b, b.Showtime, b.BookingSeats.ToList())).ToList(), "Bookings fetched");
        }

        // ── Cancel Booking ────────────────────────────────────
        public async Task<ApiResponse<string>> CancelBookingAsync(Guid bookingId, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .Include(b => b.Showtime)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return ApiResponse<string>.Fail("Booking not found", 404, "Booking");
            if (booking.Showtime != null && booking.Showtime.StartTime < DateTime.Now)
                return ApiResponse<string>.Fail("Cannot cancel booking. The movie has already begun or completed.", 400, "Time");
            if (booking.Status == BookingStatus.Cancelled)
                return ApiResponse<string>.Fail("Already cancelled", 400, "Booking");

            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(ss => ss.ShowtimeId == booking.ShowtimeId && seatIds.Contains(ss.SeatId)).ToListAsync();

            foreach (var ss in showtimeSeats) { ss.Status = SeatStatus.Available; ss.LockedByUserId = null; ss.LockedAt = null; }
            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} cancelled by User {UserId}", booking.Id, userId);
            return ApiResponse<string>.Ok("Cancelled", "Booking cancelled successfully");
        }

        // ── Save Payment ──────────────────────────────────────
        public async Task<ApiResponse<string>> SavePaymentAsync(Guid bookingId, string stripePaymentIntentId, decimal amount)
        {
            var existing = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (existing != null)
            {
                existing.StripePaymentIntentId = stripePaymentIntentId;
                existing.Status = PaymentStatus.Success;
                existing.PaidAt = DateTime.Now;
                existing.Amount = amount;
            }
            else
            {
                await _context.Payments.AddAsync(new Payment
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    StripePaymentIntentId = stripePaymentIntentId,
                    Amount = amount,
                    Status = PaymentStatus.Success,
                    PaidAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("Saved", "Payment saved successfully");
        }

        // ── Release Locked Seats ──────────────────────────────
        public async Task<ApiResponse<string>> ReleaseLockedSeatsAsync(Guid bookingId, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId && b.Status == BookingStatus.Pending);

            if (booking == null) return ApiResponse<string>.Fail("Booking not found", 404, "Booking");

            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(ss => ss.ShowtimeId == booking.ShowtimeId && seatIds.Contains(ss.SeatId)).ToListAsync();

            foreach (var ss in showtimeSeats) { ss.Status = SeatStatus.Available; ss.LockedByUserId = null; ss.LockedAt = null; }

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (payment != null) payment.Status = PaymentStatus.Failed;

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("Released", "Seats released");
        }

        // ── Get Cancelled Bookings ────────────────────────────
        public async Task<ApiResponse<List<BookingResponse>>> GetCancelledBookingsAsync(string managerId)
        {
            var cinema = await _context.Cinemas.FirstOrDefaultAsync(c => c.ManagerUserId == managerId);
            if (cinema == null) return ApiResponse<List<BookingResponse>>.Fail("Cinema not found", 404, "Cinema");

            var bookings = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .Include(b => b.User)
                .Where(b => b.Showtime.CinemaId == cinema.Id && b.Status == BookingStatus.Cancelled)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            return ApiResponse<List<BookingResponse>>.Ok(
                bookings.Select(b => MapToResponse(b, b.Showtime, b.BookingSeats.ToList())).ToList(), "Cancelled bookings fetched");
        }

        // ── Process Refund ────────────────────────────────────
        public async Task<ApiResponse<string>> ProcessRefundAsync(Guid bookingId, string managerId, string? note)
        {
            var cinema = await _context.Cinemas.FirstOrDefaultAsync(c => c.ManagerUserId == managerId);
            if (cinema == null) return ApiResponse<string>.Fail("Cinema not found", 404, "Cinema");

            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Showtime.CinemaId == cinema.Id && b.Status == BookingStatus.Cancelled);

            if (booking == null) return ApiResponse<string>.Fail("Booking not found or not cancelled", 404, "Booking");
            if (booking.RefundProcessed) return ApiResponse<string>.Fail("Refund already processed", 400, "Refund");

            booking.RefundProcessed = true;
            booking.RefundedAt = DateTime.UtcNow;
            booking.RefundNote = note?.Trim() ?? "Refund processed by cinema manager";
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("Refund processed", "Refund marked as processed successfully");
        }

        // ── Get Booking By Id ─────────────────────────────────
        public async Task<ApiResponse<BookingResponse>> GetBookingByIdAsync(Guid bookingId, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return ApiResponse<BookingResponse>.Fail("Booking not found", 404, "Booking");
            return ApiResponse<BookingResponse>.Ok(
                MapToResponse(booking, booking.Showtime, booking.BookingSeats.ToList()), "Booking fetched");
        }

        // ── SendBookingConfirmationAsync (kept for interface compat) ──
        public async Task SendBookingConfirmationAsync(Guid bookingId)
        {
            _logger.LogInformation("SendBookingConfirmationAsync called for {BookingId} — handled by background task", bookingId);
            await Task.CompletedTask;
        }

        // ── Helpers ───────────────────────────────────────────
        private static string GenerateReference() =>
            "CB" + DateTime.UtcNow.ToString("yyyyMMdd") + Random.Shared.Next(1000, 9999).ToString();

        private static BookingResponse MapToResponse(Booking booking, Showtime showtime, List<BookingSeat> seats) => new()
        {
            Id = booking.Id,
            ShowtimeId = booking.ShowtimeId,
            TicketPdfUrl = booking.TicketPdfUrl,
            UserName = booking.User?.FullName,
            BookingReference = booking.BookingReference,
            MovieTitle = showtime.Movie?.Title ?? "",
            MoviePoster = showtime.Movie?.PosterUrl ?? "",
            CinemaName = showtime.Cinema?.CinemaName ?? "",
            HallName = showtime.Hall?.HallName ?? "",
            ShowtimeStart = TimeZoneHelper.ConvertToIST(showtime.StartTime),
            ShowtimeEnd = TimeZoneHelper.ConvertToIST(showtime.EndTime),
            Seats = seats.Select(s => new BookedSeatInfo
            {
                Row = s.Seat?.Row ?? "",
                SeatNumber = s.Seat?.SeatNumber ?? 0,
                SeatType = s.SeatType,
                PricePaid = s.PricePaid
            }).ToList(),
            SubTotal = booking.SubTotal,
            ConvenienceFee = booking.ConvenienceFee,
            TotalAmount = booking.TotalAmount,
            Status = booking.Status,
            StatusLabel = booking.Status.ToString(),
            BookedAt = TimeZoneHelper.ConvertToIST(booking.BookedAt),
            RefundProcessed = booking.RefundProcessed,
            RefundedAt = booking.RefundedAt.HasValue ? TimeZoneHelper.ConvertToIST(booking.RefundedAt.Value) : null,
            RefundNote = booking.RefundNote
        };
    }
}