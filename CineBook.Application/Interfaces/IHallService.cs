using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IHallService
    {
        Task<ApiResponse<HallResponse>> CreateHallAsync(string managerId, CreateHallRequest request);
        Task<ApiResponse<List<HallResponse>>> GetMyHallsAsync(string managerId);
        Task<ApiResponse<HallResponse>> GetHallByIdAsync(Guid hallId, string managerId);
        Task<ApiResponse<HallResponse>> UpdateHallAsync(Guid hallId, string managerId, UpdateHallRequest request);
        Task<ApiResponse<string>> DeleteHallAsync(Guid hallId, string managerId);
    }
}