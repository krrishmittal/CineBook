using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, string role);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        //Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
        //Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
