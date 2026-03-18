using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Domain.Enums;
using CineBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class HallService : IHallService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HallService> _logger;

        public HallService(AppDbContext context, ILogger<HallService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Create Hall + Auto-generate Seats ────────────────
        public async Task<ApiResponse<HallResponse>> CreateHallAsync(
            string managerId, CreateHallRequest request)
        {
            // Verify manager has approved cinema
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId
                    && c.ApprovalStatus == ApprovalStatus.Approved);

            if (cinema == null)
            {
                _logger.LogWarning("CreateHallAsync failed: No approved cinema found for manager {ManagerId}", managerId);
                return ApiResponse<HallResponse>.Fail(
                    "No approved cinema found. Get your cinema approved first.", 400, "Cinema");
            }

            // Check duplicate hall name
            var exists = await _context.Halls
                .AnyAsync(h => h.CinemaId == cinema.Id && h.HallName == request.HallName);

            if (exists)
            {
                _logger.LogWarning("CreateHallAsync failed: Hall name '{HallName}' already exists in cinema {CinemaId}", request.HallName, cinema.Id);
                return ApiResponse<HallResponse>.Fail(
                    "Hall name already exists in your cinema", 400, "HallName");
            }

            // Validate rows
            if (request.StandardRows + request.PremiumRows > request.Rows)
            {
                _logger.LogWarning("CreateHallAsync failed: Standard ({StandardRows}) + Premium ({PremiumRows}) rows exceed total rows ({TotalRows})", request.StandardRows, request.PremiumRows, request.Rows);
                return ApiResponse<HallResponse>.Fail(
                    "Standard + Premium rows cannot exceed total rows", 400, "Rows");
            }

            // VULNERABILITY FIX: Prevent Memory Exhaustion / DoS attacks by capping maximum seats/rows
            if (request.Rows > 50 || request.SeatsPerRow > 100)
            {
                _logger.LogWarning("CreateHallAsync failed: Exceeded maximum allowed bounds. Rows: {Rows}, Seats: {Seats}", request.Rows, request.SeatsPerRow);
                return ApiResponse<HallResponse>.Fail("Cannot exceed 50 rows or 100 seats per row.", 400, "SizeLimit");
            }

            var totalSeats = request.Rows * request.SeatsPerRow;

            var hall = new Hall
            {
                Id = Guid.NewGuid(),
                CinemaId = cinema.Id,
                HallName = request.HallName,
                HallType = request.HallType,
                TotalSeats = totalSeats,
                IsActive = true
            };

            // Auto-generate seats
            List<Seat> seats;

            if (request.CustomSeats != null && request.CustomSeats.Any())
            {
                // Use custom designer layout
                seats = request.CustomSeats.Select(cs => new Seat
                {
                    Id = Guid.NewGuid(),
                    HallId = hall.Id,
                    Row = cs.Row,
                    SeatNumber = cs.SeatNumber,
                    SeatType = (SeatType)cs.SeatType,
                    IsActive = true
                }).ToList();

                hall.TotalSeats = seats.Count;
            }
            else
            {
                // Fallback: auto-generate from rows
                seats = new List<Seat>();
                for (int r = 0; r < request.Rows; r++)
                {
                    string rowLabel = ((char)('A' + r)).ToString();
                    SeatType seatType = r < request.StandardRows ? SeatType.Standard
                                      : r < request.StandardRows + request.PremiumRows ? SeatType.Premium
                                      : SeatType.VIP;
                    for (int s = 1; s <= request.SeatsPerRow; s++)
                    {
                        seats.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            HallId = hall.Id,
                            Row = rowLabel,
                            SeatNumber = s,
                            SeatType = seatType,
                            IsActive = true
                        });
                    }
                }
                hall.TotalSeats = seats.Count;
            }

            await _context.Halls.AddAsync(hall);
            await _context.Seats.AddRangeAsync(seats);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hall {HallId} created successfully: '{HallName}' with {Count} seats in cinema {CinemaId}",
                hall.Id, hall.HallName, hall.TotalSeats, cinema.Id);

            hall.Seats = seats;
            return ApiResponse<HallResponse>.Ok(MapToResponse(hall), "Hall created successfully");
        }

        // ── Get My Halls ──────────────────────────────────────
        public async Task<ApiResponse<List<HallResponse>>> GetMyHallsAsync(string managerId)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId
                    && c.ApprovalStatus == ApprovalStatus.Approved);

            if (cinema == null)
            {
                _logger.LogWarning("GetMyHallsAsync failed: No approved cinema found for manager {ManagerId}", managerId);
                return ApiResponse<List<HallResponse>>.Fail(
                    "No approved cinema found", 404, "Cinema");
            }

            var halls = await _context.Halls
                .Include(h => h.Seats)
                .Where(h => h.CinemaId == cinema.Id)
                .OrderBy(h => h.HallName)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} halls for cinema {CinemaId}", halls.Count, cinema.Id);

            return ApiResponse<List<HallResponse>>.Ok(
                halls.Select(MapToResponse).ToList(), "Halls fetched");
        }

        // ── Get Hall By Id ────────────────────────────────────
        public async Task<ApiResponse<HallResponse>> GetHallByIdAsync(Guid hallId, string managerId)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
            {
                _logger.LogWarning("GetHallByIdAsync failed: Cinema not found for manager {ManagerId}", managerId);
                return ApiResponse<HallResponse>.Fail("Cinema not found", 404, "Cinema");
            }

            var hall = await _context.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == hallId && h.CinemaId == cinema.Id);

            if (hall == null)
            {
                _logger.LogWarning("GetHallByIdAsync failed: Hall {HallId} not found in cinema {CinemaId}", hallId, cinema.Id);
                return ApiResponse<HallResponse>.Fail("Hall not found", 404, "Hall");
            }

            _logger.LogInformation("Hall {HallId} fetched successfully for cinema {CinemaId}", hallId, cinema.Id);

            return ApiResponse<HallResponse>.Ok(MapToResponse(hall), "Hall fetched");
        }

        // ── Update Hall ───────────────────────────────────────
        public async Task<ApiResponse<HallResponse>> UpdateHallAsync(
            Guid hallId, string managerId, UpdateHallRequest request)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
            {
                _logger.LogWarning("UpdateHallAsync failed: Cinema not found for manager {ManagerId}", managerId);
                return ApiResponse<HallResponse>.Fail("Cinema not found", 404, "Cinema");
            }

            var hall = await _context.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == hallId && h.CinemaId == cinema.Id);

            if (hall == null)
            {
                _logger.LogWarning("UpdateHallAsync failed: Hall {HallId} not found in cinema {CinemaId}", hallId, cinema.Id);
                return ApiResponse<HallResponse>.Fail("Hall not found", 404, "Hall");
            }

            hall.HallName = request.HallName;
            hall.HallType = request.HallType;
            hall.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Hall {HallId} updated successfully to '{HallName}' in cinema {CinemaId}", hallId, hall.HallName, cinema.Id);

            return ApiResponse<HallResponse>.Ok(MapToResponse(hall), "Hall updated");
        }

        // ── Delete Hall ───────────────────────────────────────
        public async Task<ApiResponse<string>> DeleteHallAsync(Guid hallId, string managerId)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
            {
                _logger.LogWarning("DeleteHallAsync failed: Cinema not found for manager {ManagerId}", managerId);
                return ApiResponse<string>.Fail("Cinema not found", 404, "Cinema");
            }

            var hall = await _context.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == hallId && h.CinemaId == cinema.Id);

            if (hall == null)
            {
                _logger.LogWarning("DeleteHallAsync failed: Hall {HallId} not found in cinema {CinemaId}", hallId, cinema.Id);
                return ApiResponse<string>.Fail("Hall not found", 404, "Hall");
            }

            // Prevent deletion if the hall has associated showtimes
            var hasShowtimes = await _context.Showtimes.AnyAsync(s => s.HallId == hallId);
            if (hasShowtimes)
            {
                _logger.LogWarning("DeleteHallAsync failed: Hall {HallId} has associated showtimes", hallId);
                return ApiResponse<string>.Fail("Cannot delete hall because it has scheduled showtimes.", 400, "Hall");
            }

            // Delete ShowtimeSeats associated with this hall's seats first
            var seatIds = hall.Seats.Select(s => s.Id).ToList();
            if (seatIds.Count > 0)
            {
                var showtimeSeats = await _context.ShowtimeSeats
                    .Where(ss => seatIds.Contains(ss.SeatId))
                    .ToListAsync();

                if (showtimeSeats.Count > 0)
                {
                    _context.ShowtimeSeats.RemoveRange(showtimeSeats);
                    _logger.LogInformation("Removed {Count} showtime seats associated with hall {HallId}", showtimeSeats.Count, hallId);
                }
            }

            _context.Halls.Remove(hall);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hall {HallId} ('{HallName}') deleted successfully by manager {ManagerId}", hallId, hall.HallName, managerId);

            return ApiResponse<string>.Ok("Hall deleted", "Hall removed successfully");
        }

        // ── Map Helper ────────────────────────────────────────
        private static HallResponse MapToResponse(Hall hall) => new()
        {
            Id = hall.Id,
            CinemaId = hall.CinemaId,
            HallName = hall.HallName,
            HallType = hall.HallType,
            HallTypeLabel = hall.HallType.ToString(),
            TotalSeats = hall.TotalSeats,
            StandardSeats = hall.Seats?.Count(s => s.SeatType == SeatType.Standard) ?? 0,
            PremiumSeats = hall.Seats?.Count(s => s.SeatType == SeatType.Premium) ?? 0,
            VIPSeats = hall.Seats?.Count(s => s.SeatType == SeatType.VIP) ?? 0,
            IsActive = hall.IsActive,
            Seats = hall.Seats?.OrderBy(s => s.Row).ThenBy(s => s.SeatNumber)
                .Select(s => new SeatResponse
                {
                    Id = s.Id,
                    Row = s.Row,
                    SeatNumber = s.SeatNumber,
                    SeatType = s.SeatType,
                    SeatTypeLabel = s.SeatType.ToString(),
                    IsActive = s.IsActive
                }).ToList() ?? new()
        };
    }
}