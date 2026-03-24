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
    public class ShowtimeService : IShowtimeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ShowtimeService> _logger;

        public ShowtimeService(AppDbContext context, ILogger<ShowtimeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Create Showtime ───────────────────────────────────
        public async Task<ApiResponse<ShowtimeResponse>> CreateShowtimeAsync(
            string managerId, CreateShowtimeRequest request)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId
                    && c.ApprovalStatus == ApprovalStatus.Approved);

            if (cinema == null)
            {
                _logger.LogWarning("CreateShowtime failed: No approved cinema found for manager {ManagerId}", managerId);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "No approved cinema found", 400, "Cinema");
            }

            var hall = await _context.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == request.HallId
                    && h.CinemaId == cinema.Id && h.IsActive);

            if (hall == null)
            {
                _logger.LogWarning("CreateShowtime failed: Hall {HallId} not found or inactive for cinema {CinemaId}", request.HallId, cinema.Id);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "Hall not found or inactive", 404, "Hall");
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == request.MovieId && !m.IsDeleted);

            if (movie == null)
            {
                _logger.LogWarning("CreateShowtime failed: Movie {MovieId} not found", request.MovieId);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "Movie not found", 404, "Movie");
            }

            var endTime = request.StartTime.AddMinutes(movie.DurationTime);

            var conflict = await _context.Showtimes
                .AnyAsync(s => s.HallId == request.HallId
                    && s.IsActive
                    && s.StartTime < endTime
                    && s.EndTime > request.StartTime);

            if (conflict)
            {
                _logger.LogWarning("CreateShowtime failed: Schedule conflict in Hall {HallId} from {StartTime} to {EndTime}", request.HallId, request.StartTime, endTime);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "Hall already has a showtime during this period", 400, "Conflict");
            }

            var showtime = new Showtime
            {
                Id = Guid.NewGuid(),
                MovieId = request.MovieId,
                HallId = request.HallId,
                CinemaId = cinema.Id,
                StartTime = TimeZoneHelper.ConvertToUTC(request.StartTime),
                EndTime = TimeZoneHelper.ConvertToUTC(request.StartTime).AddMinutes(movie.DurationTime),
                PriceStandard = request.PriceStandard,
                PricePremium = request.PricePremium,
                PriceVIP = request.PriceVIP,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Showtimes.AddAsync(showtime);

            var showtimeSeats = hall.Seats
                .Where(s => s.IsActive)
                .Select(seat => new ShowtimeSeat
                {
                    Id = Guid.NewGuid(),
                    ShowtimeId = showtime.Id,
                    SeatId = seat.Id,
                    Status = SeatStatus.Available
                }).ToList();

            await _context.ShowtimeSeats.AddRangeAsync(showtimeSeats);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Showtime {ShowtimeId} created for movie '{MovieTitle}' at {Time} in Hall {HallId}",
                showtime.Id, movie.Title, showtime.StartTime, hall.Id);

            showtime.Movie = movie;
            showtime.Hall = hall;
            showtime.Cinema = cinema;

            return ApiResponse<ShowtimeResponse>.Ok(
                MapToResponse(showtime, showtimeSeats.Count),
                "Showtime created successfully");
        }

        // ── Get My Showtimes (Paged) ──────────────────────────
        public async Task<ApiResponse<PagedResponse<ShowtimeResponse>>> GetMyShowtimesPagedAsync(
            string managerId, string? date, int page, int pageSize)
        {
            // Clamp to safe values
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId
                    && c.ApprovalStatus == ApprovalStatus.Approved);

            if (cinema == null)
            {
                _logger.LogWarning("GetMyShowtimesPaged failed: No approved cinema for manager {ManagerId}", managerId);
                return ApiResponse<PagedResponse<ShowtimeResponse>>.Fail(
                    "No approved cinema found", 404, "Cinema");
            }

            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Include(s => s.ShowtimeSeats)
                .Where(s => s.CinemaId == cinema.Id)
                .AsQueryable();

            // Optional date filter
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
                query = query.Where(s => s.StartTime.Date == filterDate.Date);

            query = query.OrderBy(s => s.StartTime);

            // Count before paging
            var totalCount = await query.CountAsync();

            var showtimes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation(
                "Paged showtime query — Cinema: {CinemaId}, Page: {Page}/{TotalPages}, PageSize: {PageSize}, Total: {Total}, Date: '{Date}'",
                cinema.Id, page,
                (int)Math.Ceiling(totalCount / (double)pageSize),
                pageSize, totalCount, date ?? "All");

            var items = showtimes.Select(s =>
                MapToResponse(s, s.ShowtimeSeats.Count(ss => ss.Status == SeatStatus.Available))
            ).ToList();

            var result = new PagedResponse<ShowtimeResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResponse<ShowtimeResponse>>.Ok(result, "Showtimes fetched");
        }

        // ── Update Showtime ───────────────────────────────────
        public async Task<ApiResponse<ShowtimeResponse>> UpdateShowtimeAsync(
            Guid id, string managerId, UpdateShowtimeRequest request)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
            {
                _logger.LogWarning("UpdateShowtime failed: Cinema not found for manager {ManagerId}", managerId);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "Cinema not found", 404, "Cinema");
            }

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.ShowtimeSeats)
                .FirstOrDefaultAsync(s => s.Id == id && s.CinemaId == cinema.Id);

            if (showtime == null)
            {
                _logger.LogWarning("UpdateShowtime failed: Showtime {ShowtimeId} not found in cinema {CinemaId}", id, cinema.Id);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "Showtime not found", 404, "Showtime");
            }

            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.ShowtimeId == id && b.Status != BookingStatus.Cancelled);

            if (hasBookings)
            {
                _logger.LogWarning("UpdateShowtime failed: Showtime {ShowtimeId} has active bookings", id);
                return ApiResponse<ShowtimeResponse>.Fail(
                    "Cannot modify this showtime because it has active bookings. Please cancel the bookings first to process refunds.", 400, "Bookings");
            }

            showtime.StartTime = TimeZoneHelper.ConvertToUTC(request.StartTime);
            showtime.EndTime = TimeZoneHelper.ConvertToUTC(request.StartTime).AddMinutes(showtime.Movie.DurationTime);
            showtime.PriceStandard = request.PriceStandard;
            showtime.PricePremium = request.PricePremium;
            showtime.PriceVIP = request.PriceVIP;
            showtime.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Showtime {ShowtimeId} updated successfully", id);

            return ApiResponse<ShowtimeResponse>.Ok(
                MapToResponse(showtime,
                    showtime.ShowtimeSeats.Count(ss => ss.Status == SeatStatus.Available)),
                "Showtime updated");
        }

        // ── Delete Showtime ───────────────────────────────────
        public async Task<ApiResponse<string>> DeleteShowtimeAsync(Guid id, string managerId)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
                return ApiResponse<string>.Fail("Cinema not found", 404, "Cinema");

            var showtime = await _context.Showtimes
                .Include(s => s.ShowtimeSeats)
                .FirstOrDefaultAsync(s => s.Id == id && s.CinemaId == cinema.Id);

            if (showtime == null)
                return ApiResponse<string>.Fail("Showtime not found", 404, "Showtime");

            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.ShowtimeId == id &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending));

            if (hasActiveBookings)
                return ApiResponse<string>.Fail(
                    "Cannot delete showtime with active bookings. Cancel bookings first.", 400, "Bookings");

            var bookingIds = await _context.Bookings
                .Where(b => b.ShowtimeId == id)
                .Select(b => b.Id)
                .ToListAsync();

            if (bookingIds.Any())
            {
                var bookingSeats = await _context.BookingSeats
                    .Where(bs => bookingIds.Contains(bs.BookingId))
                    .ToListAsync();
                _context.BookingSeats.RemoveRange(bookingSeats);

                var bookings = await _context.Bookings
                    .Where(b => bookingIds.Contains(b.Id))
                    .ToListAsync();
                _context.Bookings.RemoveRange(bookings);
            }

            _context.ShowtimeSeats.RemoveRange(showtime.ShowtimeSeats);
            _context.Showtimes.Remove(showtime);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Showtime {ShowtimeId} deleted successfully", id);

            return ApiResponse<string>.Ok("Deleted", "Showtime deleted successfully");
        }

        // ── Get Showtimes By Movie (Public) ───────────────────
        public async Task<ApiResponse<List<ShowtimeResponse>>> GetShowtimesByMovieAsync(
            Guid movieId, string? date)
        {
            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Include(s => s.ShowtimeSeats)
                .Where(s => s.MovieId == movieId && s.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
                query = query.Where(s => s.StartTime.Date == filterDate.Date);
            else
                query = query.Where(s => s.StartTime >= DateTime.UtcNow);

            var showtimes = await query.OrderBy(s => s.StartTime).ToListAsync();

            _logger.LogInformation("Retrieved {Count} public showtimes for movie {MovieId} on date {Date}",
                showtimes.Count, movieId, date ?? "Future/Today");

            return ApiResponse<List<ShowtimeResponse>>.Ok(
                showtimes.Select(s => MapToResponse(s,
                    s.ShowtimeSeats.Count(ss => ss.Status == SeatStatus.Available)
                )).ToList(), "Showtimes fetched");
        }

        // ── Map Helper ────────────────────────────────────────
        private static ShowtimeResponse MapToResponse(Showtime s, int availableSeats) => new()
        {
            Id = s.Id,
            MovieId = s.MovieId,
            MovieTitle = s.Movie?.Title ?? "",
            MoviePoster = s.Movie?.PosterUrl ?? "",
            MovieDurationTime = s.Movie?.DurationTime ?? 0,
            MovieLanguage = s.Movie?.Language ?? "",
            HallId = s.HallId,
            HallName = s.Hall?.HallName ?? "",
            HallType = s.Hall?.HallType.ToString() ?? "",
            CinemaId = s.CinemaId,
            CinemaName = s.Cinema?.CinemaName ?? "Unknown Cinema",
            StartTime = TimeZoneHelper.ConvertToIST(s.StartTime),
            EndTime = TimeZoneHelper.ConvertToIST(s.EndTime),
            PriceStandard = s.PriceStandard,
            PricePremium = s.PricePremium,
            PriceVIP = s.PriceVIP,
            IsActive = s.IsActive,
            TotalSeats = s.Hall?.TotalSeats ?? 0,
            AvailableSeats = availableSeats,
            CreatedAt = TimeZoneHelper.ConvertToIST(s.CreatedAt)
        };
    }
}