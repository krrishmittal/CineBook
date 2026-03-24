using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IMovieService
    {
        Task<ApiResponse<MovieResponse>> CreateAsync(CreateMovieRequest request);
        // IMovieService.cs
        Task<ApiResponse<PagedResponse<MovieResponse>>> GetAllPagedAsync(
            string? search, string? genre, string? status, int page = 1, int pageSize = 18);
        Task<ApiResponse<MovieResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<MovieResponse>> UpdateAsync(Guid id, UpdateMovieRequest request);
        Task<ApiResponse<string>> DeleteAsync(Guid id);
    }
}