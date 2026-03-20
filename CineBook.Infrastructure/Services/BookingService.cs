using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Domain.Enums;
using CineBook.Infrastructure.Persistence;
using CineBook.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookingService> _logger;
        private const decimal ConvenienceFeeRate = 0.02m; // 2%

        public BookingService(AppDbContext context, ILogger<BookingService> logger)
        {
            _context = context;
            _logger = logger;
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
                return ApiResponse<SeatLayoutResponse>.Fail(
                    "Showtime not found", 404, "GetSeatLayoutAsync");
            }

            // Release expired locks (older than 10 minutes)
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

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // VULNERABILITY BENIGN: Ignore concurrency. Another parallel request already safely released the lock.
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

            _logger.LogInformation("Seat layout fetched successfully for Showtime {ShowtimeId} by User {UserId}", showtimeId, userId);

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

        // ── Initiate Booking (Lock seats + create pending booking) ──
        public async Task<ApiResponse<BookingResponse>> InitiateBookingAsync(
            string userId, InitiateBookingRequest request)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Include(s => s.ShowtimeSeats)
                    .ThenInclude(ss => ss.Seat)
                .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId && s.IsActive);

            if (showtime == null)
            {
                _logger.LogWarning("InitiateBookingAsync failed: Showtime {ShowtimeId} not found", request.ShowtimeId);
                return ApiResponse<BookingResponse>.Fail(
                    "Showtime not found", 404, "Showtime");
            }

            if (!request.SeatIds.Any())
            {
                _logger.LogWarning("InitiateBookingAsync failed: No seats selected by User {UserId} for Showtime {ShowtimeId}", userId, request.ShowtimeId);
                return ApiResponse<BookingResponse>.Fail(
                    "Select at least one seat", 400, "Seats");
            }

            if (request.SeatIds.Count > 10)
            {
                _logger.LogWarning("InitiateBookingAsync failed: Maximum seats exceeded ({Count}) by User {UserId} for Showtime {ShowtimeId}", request.SeatIds.Count, userId, request.ShowtimeId);
                return ApiResponse<BookingResponse>.Fail(
                    "Maximum 10 seats per booking", 400, "Seats");
            }

            // Validate seats are available
            var showtimeSeats = showtime.ShowtimeSeats
                .Where(ss => request.SeatIds.Contains(ss.SeatId))
                .ToList();

            if (showtimeSeats.Count != request.SeatIds.Count)
            {
                _logger.LogWarning("InitiateBookingAsync failed: One or more requested seats not found in Showtime {ShowtimeId}", request.ShowtimeId);
                return ApiResponse<BookingResponse>.Fail(
                    "One or more seats not found", 400, "Seats");
            }

            var unavailable = showtimeSeats
                .Where(ss => ss.Status != SeatStatus.Available
                    && !(ss.Status == SeatStatus.Locket && ss.LockedByUserId == userId))
                .ToList();

            if (unavailable.Any())
            {
                _logger.LogWarning("InitiateBookingAsync failed: {Count} requested seats are already taken or locked by others for Showtime {ShowtimeId}", unavailable.Count, request.ShowtimeId);
                return ApiResponse<BookingResponse>.Fail(
                    "One or more seats are already taken", 400, "Seats");
            }

            // Cancel any existing pending booking by this user for this showtime
            var existingBooking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.UserId == userId
                    && b.ShowtimeId == request.ShowtimeId
                    && b.Status == BookingStatus.Pending);

            if (existingBooking != null)
            {
                _logger.LogInformation("Releasing previous pending booking {BookingId} locks for User {UserId} on Showtime {ShowtimeId}", existingBooking.Id, userId, request.ShowtimeId);

                // Release old locks
                var oldSeatIds = existingBooking.BookingSeats.Select(bs => bs.SeatId).ToList();
                var oldLocks = showtime.ShowtimeSeats
                    .Where(ss => oldSeatIds.Contains(ss.SeatId)).ToList();
                foreach (var l in oldLocks)
                {
                    l.Status = SeatStatus.Available;
                    l.LockedByUserId = null;
                    l.LockedAt = null;
                }
                _context.Bookings.Remove(existingBooking);
            }

            // Lock seats
            foreach (var ss in showtimeSeats)
            {
                ss.Status = SeatStatus.Locket;
                ss.LockedByUserId = userId;
                ss.LockedAt = DateTime.UtcNow;
            }

            // Calculate prices
            decimal subTotal = showtimeSeats.Sum(ss =>
                ss.Seat.SeatType == SeatType.Standard ? showtime.PriceStandard
                : ss.Seat.SeatType == SeatType.Premium ? showtime.PricePremium
                : showtime.PriceVIP);

            decimal convenienceFee = Math.Round(subTotal * ConvenienceFeeRate, 2);
            decimal totalAmount = subTotal + convenienceFee;

            // Create booking
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
            var pendingPayment = new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                StripePaymentIntentId = "",   
                Amount = totalAmount,
                Status = PaymentStatus.Pending,
                PaidAt = null
            };
            await _context.Payments.AddAsync(pendingPayment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} (Ref: {Ref}) initiated by User {UserId} with {Count} locked seats for Showtime {ShowtimeId}",
                booking.Id, booking.BookingReference, userId, bookingSeats.Count, request.ShowtimeId);

            booking.BookingSeats = bookingSeats;

            return ApiResponse<BookingResponse>.Ok(
                MapToResponse(booking, showtime, bookingSeats),
                "Seats locked! Proceed to payment.");
        }

        // ── Confirm Booking ───────────────────────────────────
        public async Task<ApiResponse<BookingResponse>> ConfirmBookingAsync(
            string userId, ConfirmBookingRequest request)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Hall)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId
                    && b.UserId == userId
                    && b.Status == BookingStatus.Pending);

            if (booking == null)
            {
                _logger.LogWarning("ConfirmBookingAsync failed: Booking {BookingId} not found, already processed, or doesn't belong to User {UserId}", request.BookingId, userId);
                return ApiResponse<BookingResponse>.Fail(
                    "Booking not found or already processed", 404, "Booking");
            }

            // Mark seats as booked
            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(ss => ss.ShowtimeId == booking.ShowtimeId
                    && seatIds.Contains(ss.SeatId))
                .ToListAsync();

            // VULNERABILITY FIX: Double-booking protection. Verify seats weren't snatched internally if locks expired during gateway delay
            var snatchedSeats = showtimeSeats.Where(ss =>
                ss.Status == SeatStatus.Booked ||
                (ss.Status == SeatStatus.Locket && ss.LockedByUserId != userId)
            ).ToList();

            if (snatchedSeats.Any())
            {
                _logger.LogWarning("ConfirmBookingAsync failed: {Count} seats were snatched for User {UserId} in Booking {BookingId}", snatchedSeats.Count, userId, request.BookingId);

                // Auto-cancel the compromised pending booking to start refund process
                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();

                return ApiResponse<BookingResponse>.Fail(
                    "Your seat lock expired during checkout and one or more seats were claimed. Your order was cancelled, and any payment will be refunded.", 400, "Seats");
            }

            foreach (var ss in showtimeSeats)
            {
                ss.Status = SeatStatus.Booked;
                ss.LockedByUserId = null;
                ss.LockedAt = null;
            }

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} (Ref: {Ref}) confirmed successfully for User {UserId}",
                booking.Id, booking.BookingReference, userId);

            return ApiResponse<BookingResponse>.Ok(
                MapToResponse(booking, booking.Showtime, booking.BookingSeats.ToList()),
                "Booking confirmed!");
        }

        // ── Get My Bookings ───────────────────────────────────
        public async Task<ApiResponse<List<BookingResponse>>> GetMyBookingsAsync(string userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Hall)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} bookings for User {UserId}", bookings.Count, userId);

            return ApiResponse<List<BookingResponse>>.Ok(
                bookings.Select(b =>
                    MapToResponse(b, b.Showtime, b.BookingSeats.ToList())).ToList(),
                "Bookings fetched");
        }

        public async Task<ApiResponse<List<BookingResponse>>> GetCinemaBookingsAsync(
            string managerId, string? date)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
                return ApiResponse<List<BookingResponse>>.Fail("Cinema not found", 404, "Cinema");

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
                bookings.Select(b => MapToResponse(b, b.Showtime, b.BookingSeats.ToList())).ToList(),
                "Bookings fetched");
        }

        // ── Cancel Booking ────────────────────────────────────
        public async Task<ApiResponse<string>> CancelBookingAsync(
            Guid bookingId, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .Include(b => b.Showtime)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                _logger.LogWarning("CancelBookingAsync failed: Booking {BookingId} not found or doesn't belong to User {UserId}", bookingId, userId);
                return ApiResponse<string>.Fail("Booking not found", 404, "Booking");
            }

            // VULNERABILITY FIX: Block canceling and refunding shows that have already begun or passed
            if (booking.Showtime != null && booking.Showtime.StartTime < DateTime.Now)
            {
                _logger.LogWarning("CancelBookingAsync failed: User {UserId} attempted to cancel a past booking {BookingId}", userId, bookingId);
                return ApiResponse<string>.Fail("Cannot cancel booking. The movie has already begun or completed.", 400, "Time");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                _logger.LogWarning("CancelBookingAsync failed: Booking {BookingId} is already cancelled", bookingId);
                return ApiResponse<string>.Fail("Already cancelled", 400, "Booking");
            }

            // Release seats
            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(ss => ss.ShowtimeId == booking.ShowtimeId
                    && seatIds.Contains(ss.SeatId))
                .ToListAsync();

            foreach (var ss in showtimeSeats)
            {
                ss.Status = SeatStatus.Available;
                ss.LockedByUserId = null;
                ss.LockedAt = null;
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} (Ref: {Ref}) cancelled successfully by User {UserId}, {Count} seats released",
                booking.Id, booking.BookingReference, userId, showtimeSeats.Count);

            return ApiResponse<string>.Ok("Cancelled", "Booking cancelled successfully");
        }

        public async Task<ApiResponse<string>> SavePaymentAsync(
    Guid bookingId, string stripePaymentIntentId, decimal amount)
        {
            // Check if payment already saved (prevent duplicate)
            var existing = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (existing != null)
            {
                // Update if exists
                existing.StripePaymentIntentId = stripePaymentIntentId;
                existing.Status = PaymentStatus.Success;
                existing.PaidAt = DateTime.Now;
                existing.Amount = amount;
            }
            else
            {
                // Create new payment record
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    StripePaymentIntentId = stripePaymentIntentId,
                    Amount = amount,
                    Status = PaymentStatus.Success,
                    PaidAt = DateTime.Now
                };
                await _context.Payments.AddAsync(payment);
            }

            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("Saved", "Payment saved successfully");
        }

        // ── Helpers ───────────────────────────────────────────
        private static string GenerateReference()
        {
            return "CB" + DateTime.UtcNow.ToString("yyyyMMdd")
                + Random.Shared.Next(1000, 9999).ToString();
        }


        public async Task<ApiResponse<List<BookingResponse>>> GetCancelledBookingsAsync(string managerId)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
                return ApiResponse<List<BookingResponse>>.Fail("Cinema not found", 404, "Cinema");

            var bookings = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .Include(b => b.User)
                .Where(b => b.Showtime.CinemaId == cinema.Id
                         && b.Status == BookingStatus.Cancelled)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            return ApiResponse<List<BookingResponse>>.Ok(
                bookings.Select(b => MapToResponse(b, b.Showtime, b.BookingSeats.ToList())).ToList(),
                "Cancelled bookings fetched");
        }

        public async Task<ApiResponse<string>> ProcessRefundAsync(
            Guid bookingId, string managerId, string? note)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
                return ApiResponse<string>.Fail("Cinema not found", 404, "Cinema");

            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .FirstOrDefaultAsync(b => b.Id == bookingId
                    && b.Showtime.CinemaId == cinema.Id
                    && b.Status == BookingStatus.Cancelled);

            if (booking == null)
                return ApiResponse<string>.Fail("Booking not found or not cancelled", 404, "Booking");

            if (booking.RefundProcessed)
                return ApiResponse<string>.Fail("Refund already processed", 400, "Refund");

            booking.RefundProcessed = true;
            booking.RefundedAt = DateTime.UtcNow;  
            booking.RefundNote = note?.Trim() ?? "Refund processed by cinema manager";

            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("Refund processed", "Refund marked as processed successfully");
        }


        public async Task<ApiResponse<BookingResponse>> GetBookingByIdAsync(
        Guid bookingId, string userId) {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return ApiResponse<BookingResponse>.Fail(
                    "Booking not found", 404, "Booking");

            return ApiResponse<BookingResponse>.Ok(
                MapToResponse(booking, booking.Showtime, booking.BookingSeats.ToList()),
                "Booking fetched");
        }
        public async Task<ApiResponse<string>> ReleaseLockedSeatsAsync(
    Guid bookingId, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.Id == bookingId
                    && b.UserId == userId
                    && b.Status == BookingStatus.Pending);

            if (booking == null)
                return ApiResponse<string>.Fail("Booking not found", 404, "Booking");

            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(ss => ss.ShowtimeId == booking.ShowtimeId
                    && seatIds.Contains(ss.SeatId))
                .ToListAsync();

            foreach (var ss in showtimeSeats)
            {
                ss.Status = SeatStatus.Available;
                ss.LockedByUserId = null;
                ss.LockedAt = null;
            }

            // Update payment to cancelled
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (payment != null)
                payment.Status = PaymentStatus.Failed;

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("Released", "Seats released");
        }


        private static BookingResponse MapToResponse(Booking booking, Showtime showtime, List<BookingSeat> seats) => new(){
                Id = booking.Id,
                ShowtimeId=booking.ShowtimeId,
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
                RefundedAt = booking.RefundedAt.HasValue
                    ? TimeZoneHelper.ConvertToIST(booking.RefundedAt.Value)
                : null,
                RefundNote = booking.RefundNote
        };
    }

}