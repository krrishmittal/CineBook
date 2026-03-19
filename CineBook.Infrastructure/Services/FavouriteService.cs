using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Infrastructure.Services
{
    public class FavouriteService : IFavouriteService
    {
        private readonly AppDbContext _context;

        public FavouriteService(AppDbContext context)
        {
            _context = context;
        }

        // Toggle — add if not exists, remove if exists
        public async Task<ApiResponse<bool>> ToggleFavouriteAsync(
            string userId, Guid movieId)
        {
            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == movieId && !m.IsDeleted);

            if (movie == null)
                return ApiResponse<bool>.Fail("Movie not found", 404, "Movie");

            var existing = await _context.UserFavourites
                .FirstOrDefaultAsync(f => f.UserId == userId
                    && f.MovieId == movieId);

            if (existing != null)
            {
                // Remove favourite
                _context.UserFavourites.Remove(existing);
                await _context.SaveChangesAsync();
                return ApiResponse<bool>.Ok(false, "Removed from favourites");
            }
            else
            {
                // Add favourite
                var fav = new UserFavourite
                {
                    UserId = userId,
                    MovieId = movieId,
                    AddedAt = DateTime.Now
                };
                await _context.UserFavourites.AddAsync(fav);
                await _context.SaveChangesAsync();
                return ApiResponse<bool>.Ok(true, "Added to favourites");
            }
        }

        // Get all favourites with movie details
        public async Task<ApiResponse<List<FavouriteResponse>>> GetMyFavouritesAsync(
            string userId)
        {
            var favourites = await _context.UserFavourites
                .Include(f => f.Movie)
                .Where(f => f.UserId == userId && !f.Movie.IsDeleted)
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            return ApiResponse<List<FavouriteResponse>>.Ok(
                favourites.Select(f => new FavouriteResponse
                {
                    MovieId = f.MovieId,
                    Title = f.Movie.Title,
                    PosterUrl = f.Movie.PosterUrl ?? "",
                    Genre = f.Movie.Genre,
                    Language = f.Movie.Language,
                    DurationTime = f.Movie.DurationTime,
                    CertificateRating = f.Movie.CertificateRating,
                    Status = f.Movie.Status.ToString(),
                    AddedAt = f.AddedAt
                }).ToList(), "Favourites fetched");
        }

        // Get just IDs — used to check which movies are hearted
        public async Task<ApiResponse<List<Guid>>> GetFavouriteIdsAsync(string userId)
        {
            var ids = await _context.UserFavourites
                .Where(f => f.UserId == userId)
                .Select(f => f.MovieId)
                .ToListAsync();

            return ApiResponse<List<Guid>>.Ok(ids, "Favourite IDs fetched");
        }
    }
}   