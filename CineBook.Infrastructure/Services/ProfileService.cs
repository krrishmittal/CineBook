using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(UserManager<ApplicationUser> userManager, ILogger<ProfileService> logger)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<ApiResponse<ProfileResponse>> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetProfileAsync failed: User {UserId} not found", userId);
                return ApiResponse<ProfileResponse>.Fail("User not Found", 404, "GetProfileAsync");
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

            _logger.LogInformation("Profile fetched successfully for user {UserId}", userId);

            return ApiResponse<ProfileResponse>.Ok(new ProfileResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                CreatedAt = user.CreatedAt
            }, "Profile fetched successfully");
        }

        public async Task<ApiResponse<ProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdateProfileAsync failed: User {UserId} not found", userId);
                return ApiResponse<ProfileResponse>.Fail("User not found", 404, "UpdateProfileAsync");
            }

            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
            if (phoneExists)
            {
                _logger.LogWarning("UpdateProfileAsync failed: Phone number {PhoneNumber} is already in use by another user", request.PhoneNumber);
                return ApiResponse<ProfileResponse>.Fail("Phone number already in use", 409, "PhoneNumber");
            }

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var error = result.Errors.First();
                _logger.LogWarning("UpdateProfileAsync failed for user {UserId}: {ErrorDescription}", userId, error.Description);
                return ApiResponse<ProfileResponse>.Fail(error.Description, 400, "Identity");
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

            return ApiResponse<ProfileResponse>.Ok(new ProfileResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                CreatedAt = user.CreatedAt
            }, "Profile updated successfully");
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                _logger.LogWarning("ChangePasswordAsync failed: Passwords do not match for user {UserId}", userId);
                return ApiResponse<string>.Fail(
                    "Passwords do not match", 400, "ConfirmPassword");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ChangePasswordAsync failed: User {UserId} not found", userId);
                return ApiResponse<string>.Fail("User not found", 404, "ChangePasswordAsync");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var error = result.Errors.First();
                _logger.LogWarning("ChangePasswordAsync failed for user {UserId}: {ErrorDescription}", userId, error.Description);
                return ApiResponse<string>.Fail(error.Description, 400, "Password");
            }

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return ApiResponse<string>.Ok(
               "Password changed successfully",
               "Your password has been updated");
        }
    }
}