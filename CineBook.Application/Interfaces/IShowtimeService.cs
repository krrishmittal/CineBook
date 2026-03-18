using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IShowtimeService
    {
        Task<ApiResponse<ShowtimeResponse>> CreateShowtimeAsync(string managerId, CreateShowtimeRequest request);
        Task<ApiResponse<List<ShowtimeResponse>>> GetMyShowtimesAsync(string managerId, string? date);
        Task<ApiResponse<ShowtimeResponse>> UpdateShowtimeAsync(Guid id, string managerId, UpdateShowtimeRequest request);
        Task<ApiResponse<string>> DeleteShowtimeAsync(Guid id, string managerId);

        // For public movie detail page later
        Task<ApiResponse<List<ShowtimeResponse>>> GetShowtimesByMovieAsync(Guid movieId, string? date);
    }
}