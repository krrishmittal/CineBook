using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IMovieService
    {
        Task<ApiResponse<MovieResponse>> CreateAsync(CreateMovieRequest request);
        Task<ApiResponse<List<MovieResponse>>> GetAllAsync(string? search, string? genre, string? status);
        Task<ApiResponse<MovieResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<MovieResponse>> UpdateAsync(Guid id, UpdateMovieRequest request);
        Task<ApiResponse<string>> DeleteAsync(Guid id);
    }
}