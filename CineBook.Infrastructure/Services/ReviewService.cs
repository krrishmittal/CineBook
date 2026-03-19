using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ReviewResponse>> CreateReviewAsync(
            string userId, CreateReviewRequest request)
        {
            if (request.Rating < 1 || request.Rating > 5)
                return ApiResponse<ReviewResponse>.Fail(
                    "Rating must be between 1 and 5", 400, "Rating");

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == request.MovieId && !m.IsDeleted);

            if (movie == null)
                return ApiResponse<ReviewResponse>.Fail("Movie not found", 404, "Movie");

            // Check if already reviewed
            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MovieId == request.MovieId
                    && r.UserId == userId);

            if (existing != null)
                return ApiResponse<ReviewResponse>.Fail(
                    "You have already reviewed this movie", 400, "Review");

            // Check if user has a confirmed booking for this movie
            var hasVerifiedBooking = await _context.Bookings
                .AnyAsync(b => b.UserId == userId
                    && b.Status == Domain.Enums.BookingStatus.Confirmed
                    && b.Showtime.MovieId == request.MovieId);

            var user = await _context.Users.FindAsync(userId);

            var review = new Review
            {
                Id = Guid.NewGuid(),
                MovieId = request.MovieId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim() ?? "",
                IsVerifiedBooking = hasVerifiedBooking,
                CreatedAt = DateTime.Now
            };

            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            return ApiResponse<ReviewResponse>.Ok(new ReviewResponse
            {
                Id = review.Id,
                MovieId = review.MovieId,
                UserId = review.UserId,
                UserName = user?.FullName ?? "User",
                Rating = review.Rating,
                Comment = review.Comment,
                IsVerifiedBooking = review.IsVerifiedBooking,
                CreatedAt = review.CreatedAt,
                IsMyReview = true
            }, "Review submitted successfully");
        }

        public async Task<ApiResponse<List<ReviewResponse>>> GetMovieReviewsAsync(
            Guid movieId, string? userId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.MovieId == movieId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ApiResponse<List<ReviewResponse>>.Ok(
                reviews.Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    MovieId = r.MovieId,
                    UserId = r.UserId,
                    UserName = r.User?.FullName ?? "User",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsVerifiedBooking = r.IsVerifiedBooking,
                    CreatedAt = r.CreatedAt,
                    IsMyReview = r.UserId == userId
                }).ToList(), "Reviews fetched");
        }

        public async Task<ApiResponse<string>> DeleteReviewAsync(
            Guid reviewId, string userId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
                return ApiResponse<string>.Fail("Review not found", 404, "Review");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("Deleted", "Review deleted");
        }
    }
}