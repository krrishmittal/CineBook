using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Domain.Enums;
using CineBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class CinemaService : ICinemaService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CinemaService> _logger;

        public CinemaService(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<CinemaService> logger)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<ApiResponse<CinemaResponse>> RegisterCinemaAsync(string managerId, RegisterCinemaRequest request)
        {
            var existing = await _context.Cinemas.FirstOrDefaultAsync(c => c.ManagerUserId == managerId);
            if (existing != null)
            {
                _logger.LogWarning("RegisterCinemaAsync failed: Manager {ManagerId} already has a registered cinema", managerId);
                return ApiResponse<CinemaResponse>.Fail("You already have a registered cinema", 400, "RegisterCinemaAsync");
            }

            var licenseExists = await _context.Cinemas
                .AnyAsync(c => c.LicenseNumber == request.LicenseNumber);

            if (licenseExists)
            {
                _logger.LogWarning("RegisterCinemaAsync failed: License number {LicenseNumber} is already registered", request.LicenseNumber);
                return ApiResponse<CinemaResponse>.Fail(
                    "License number already registered", 400, "LicenseNumber");
            }

            var cinema = new Cinema
            {
                Id = Guid.NewGuid(),
                ManagerUserId = managerId,
                CinemaName = request.CinemaName,
                Address = request.Address,
                City = request.City,
                State = request.State,
                PinCode = request.PinCode,
                GoogleMapsLink = request.GoogleMapsLink,
                CinemaLogo = request.CinemaLogo,
                LicenseNumber = request.LicenseNumber,
                ApprovalStatus = ApprovalStatus.Pending,
                RegisteredAt = DateTime.UtcNow
            };
            await _context.Cinemas.AddAsync(cinema);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cinema {CinemaId} registered successfully: '{Name}' by manager {ManagerId}",
                cinema.Id, cinema.CinemaName, managerId);

            var manager = await _userManager.FindByIdAsync(managerId);
            return ApiResponse<CinemaResponse>.Ok(MapToResponse(cinema, manager), "Cinema registered!  Awaiting admin approval.");
        }

        public async Task<ApiResponse<CinemaResponse>> GetMyCinemaAsync(string managerId)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.Manager)
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerId);

            if (cinema == null)
            {
                _logger.LogWarning("GetMyCinemaAsync failed: No cinema found for manager {ManagerId}", managerId);
                return ApiResponse<CinemaResponse>.Fail(
                    "No cinema registered yet", 404, "Cinema");
            }

            _logger.LogInformation("Cinema {CinemaId} fetched successfully for manager {ManagerId}", cinema.Id, managerId);

            return ApiResponse<CinemaResponse>.Ok(
                MapToResponse(cinema, cinema.Manager), "Cinema fetched");
        }

        public async Task<ApiResponse<List<CinemaResponse>>> GetAllCinemasAsync(string? status)
        {
            var query = _context.Cinemas.Include(c => c.Manager).AsQueryable();

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<ApprovalStatus>(status, out var approvalStatus))
                query = query.Where(c => c.ApprovalStatus == approvalStatus);

            var cinemas = await query
                .OrderByDescending(c => c.RegisteredAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} cinemas with status filter: '{Status}'", cinemas.Count, status ?? "All");

            return ApiResponse<List<CinemaResponse>>.Ok(
                cinemas.Select(c => MapToResponse(c, c.Manager)).ToList(),
                "Cinemas fetched");
        }

        public async Task<ApiResponse<CinemaResponse>> ApproveCinemaAsync(
            Guid cinemaId, ApproveCinemaRequest request)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.Manager)
                .FirstOrDefaultAsync(c => c.Id == cinemaId);

            if (cinema == null)
            {
                _logger.LogWarning("ApproveCinemaAsync failed: Cinema {CinemaId} not found", cinemaId);
                return ApiResponse<CinemaResponse>.Fail(
                    "Cinema not found", 404, "Cinema");
            }

            cinema.ApprovalStatus = request.IsApproved
                ? ApprovalStatus.Approved
                : ApprovalStatus.Rejected;

            cinema.RejectionReason = request.IsApproved ? null : request.RejectionReason;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cinema {CinemaId} ('{Name}') was {Status} by an admin",
                cinema.Id,
                cinema.CinemaName,
                request.IsApproved ? "approved" : "rejected");

            return ApiResponse<CinemaResponse>.Ok(
                MapToResponse(cinema, cinema.Manager),
                request.IsApproved ? "Cinema approved!" : "Cinema rejected.");
        }

        // ── Map Helper ────────────────────────────────────────
        private static CinemaResponse MapToResponse(Cinema cinema, ApplicationUser? manager) => new()
        {
            Id = cinema.Id,
            CinemaName = cinema.CinemaName,
            Address = cinema.Address,
            City = cinema.City,
            State = cinema.State,
            PinCode = cinema.PinCode,
            GoogleMapsLink = cinema.GoogleMapsLink,
            CinemaLogo = cinema.CinemaLogo,
            LicenseNumber = cinema.LicenseNumber,
            ApprovalStatus = cinema.ApprovalStatus,
            ApprovalStatusLabel = cinema.ApprovalStatus.ToString(),
            RejectionReason = cinema.RejectionReason,
            ManagerId = cinema.ManagerUserId,
            ManagerName = manager?.FullName ?? "Unknown",
            ManagerPhone = manager?.PhoneNumber ?? "Unknown",
            RegisteredAt = cinema.RegisteredAt
        };
    }
}