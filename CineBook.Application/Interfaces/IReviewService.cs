using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IReviewService
    {
        Task<ApiResponse<ReviewResponse>> CreateReviewAsync(string userId, CreateReviewRequest request);
        Task<ApiResponse<List<ReviewResponse>>> GetMovieReviewsAsync(Guid movieId, string? userId);
        Task<ApiResponse<string>> DeleteReviewAsync(Guid reviewId, string userId);
    }
}