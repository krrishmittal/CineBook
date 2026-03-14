using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IProfileService
    {
        Task<ApiResponse<ProfileResponse>> GetProfileAsync(string userId);
        Task<ApiResponse<ProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
        Task<ApiResponse<string>>ChangePasswordAsync(string userId,ChangePasswordRequest request);
    }
}
