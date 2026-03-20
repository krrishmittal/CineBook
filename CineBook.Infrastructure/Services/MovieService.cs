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
    public class MovieService : IMovieService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MovieService> _logger;

        public MovieService(AppDbContext context, ILogger<MovieService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Create ────────────────────────────────────────────
        public async Task<ApiResponse<MovieResponse>> CreateAsync(CreateMovieRequest request)
        {
            var exists = await _context.Movies.AnyAsync(m => m.Title == request.Title);
            if (exists)
            {
                _logger.LogError("Movie with title '{Title}' already exists", request.Title);
                return ApiResponse<MovieResponse>.Fail(
                    "A movie with this title already exists", 400, "Movie");
            }

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Language = request.Language,
                Genre = request.Genre,
                Cast = request.Cast,
                Director = request.Director,
                DurationTime = request.DurationTime,
                CertificateRating = request.CertificateRating,
                PosterUrl = request.PosterUrl,
                TrailerUrl = request.TrailerUrl,
                ReleaseDate = request.ReleaseDate,
                Status = request.Status,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Movies.AddAsync(movie);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Movie {MovieId} created successfully: '{Title}'", movie.Id, movie.Title);

            return ApiResponse<MovieResponse>.Ok(
                MapToResponse(movie), "Movie created successfully");
        }

        // ── Get All ───────────────────────────────────────────
        public async Task<ApiResponse<List<MovieResponse>>> GetAllAsync(string? search, string? genre, string? status)
        {
            var query = _context.Movies.Where(m => !m.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m =>
                    m.Title.Contains(search) ||
                    m.Director.Contains(search) ||
                    m.Cast.Contains(search));

            if (!string.IsNullOrEmpty(genre))
                query = query.Where(m => m.Genre.Contains(genre));

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<MovieStatus>(status, out var movieStatus))
                query = query.Where(m => m.Status == movieStatus);

            var movies = await query
                .OrderBy(m => m.Title)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} movies with filters - Search: '{Search}', Genre: '{Genre}', Status: '{Status}'",
                movies.Count, search ?? "None", genre ?? "None", status ?? "None");

            return ApiResponse<List<MovieResponse>>.Ok(
                movies.Select(MapToResponse).ToList(),
                "Movies fetched successfully");
        }

        // ── Get By Id ─────────────────────────────────────────
        public async Task<ApiResponse<MovieResponse>> GetByIdAsync(Guid id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                _logger.LogWarning("GetByIdAsync failed: Movie {MovieId} not found", id);
                return ApiResponse<MovieResponse>.Fail(
                    "Movie not found", 404, "Movie");
            }

            _logger.LogInformation("Movie {MovieId} fetched successfully", id);

            return ApiResponse<MovieResponse>.Ok(
                MapToResponse(movie), "Movie fetched successfully");
        }

        // ── Update ────────────────────────────────────────────
        public async Task<ApiResponse<MovieResponse>> UpdateAsync(
            Guid id, UpdateMovieRequest request)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                _logger.LogWarning("UpdateAsync failed: Movie {MovieId} not found", id);
                return ApiResponse<MovieResponse>.Fail(
                    "Movie not found", 404, "Movie");
            }

            movie.Title = request.Title;
            movie.Description = request.Description;
            movie.Language = request.Language;
            movie.Genre = request.Genre;
            movie.Cast = request.Cast;
            movie.Director = request.Director;
            movie.DurationTime = request.DurationTime;
            movie.CertificateRating = request.CertificateRating;
            movie.PosterUrl = request.PosterUrl;
            movie.TrailerUrl = request.TrailerUrl;
            movie.ReleaseDate = request.ReleaseDate;
            movie.Status = request.Status;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Movie {MovieId} updated successfully: '{Title}'", id, movie.Title);

            return ApiResponse<MovieResponse>.Ok(
                MapToResponse(movie), "Movie updated successfully");
        }

        // ── Delete (Soft) ─────────────────────────────────────
        public async Task<ApiResponse<string>> DeleteAsync(Guid id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                _logger.LogWarning("DeleteAsync failed: Movie {MovieId} not found", id);
                return ApiResponse<string>.Fail(
                    "Movie not found", 404, "Movie");
            }

            // VULNERABILITY FIX: Prevent deleting a movie that has active upcoming scheduled showtimes
            var hasActiveShowtimes = await _context.Showtimes
                .AnyAsync(s => s.MovieId == id && s.IsActive && s.StartTime > DateTime.UtcNow);

            if (hasActiveShowtimes)
            {
                _logger.LogWarning("DeleteAsync failed: Attempted to delete active Movie {MovieId}", id);
                return ApiResponse<string>.Fail("Cannot delete a movie that is actively linked to upcoming schedules.", 400, "Movie");
            }

            movie.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Movie {MovieId} soft deleted successfully: '{Title}'", id, movie.Title);

            return ApiResponse<string>.Ok(
                "Movie deleted successfully", "Movie has been removed");
        }

        // ── Map Helper ────────────────────────────────────────
        private static MovieResponse MapToResponse(Movie movie) => new()
        {
            Id = movie.Id,
            Title = movie.Title,
            Description = movie.Description,
            Language = movie.Language,
            Genre = movie.Genre,
            Cast = movie.Cast,
            Director = movie.Director,
            DurationTime = movie.DurationTime,
            CertificateRating = movie.CertificateRating,
            PosterUrl = movie.PosterUrl,
            TrailerUrl = movie.TrailerUrl,
            ReleaseDate = movie.ReleaseDate,
            Status = movie.Status,
            StatusLabel = movie.Status.ToString(),
            CreatedAt = movie.CreatedAt
        };
    }
}