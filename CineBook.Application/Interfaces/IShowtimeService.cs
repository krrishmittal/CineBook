using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IShowtimeService
    {
        Task<ApiResponse<ShowtimeResponse>> CreateShowtimeAsync(string managerId, CreateShowtimeRequest request);

        // Replaces GetMyShowtimesAsync — returns a paged result
        Task<ApiResponse<PagedResponse<ShowtimeResponse>>> GetMyShowtimesPagedAsync(
            string managerId, string? date, int page, int pageSize);

        Task<ApiResponse<ShowtimeResponse>> UpdateShowtimeAsync(Guid id, string managerId, UpdateShowtimeRequest request);
        Task<ApiResponse<string>> DeleteShowtimeAsync(Guid id, string managerId);

        // Public — movie detail page (no pagination needed here, date-scoped)
        Task<ApiResponse<List<ShowtimeResponse>>> GetShowtimesByMovieAsync(Guid movieId, string? date);
    }
}