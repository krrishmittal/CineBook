using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ISmsService _smsService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context,
            IJwtService jwtService,
            ISmsService smsService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _jwtService = jwtService;
            _smsService = smsService;
            _logger = logger;
        }

        // ── Register ─────────────────────────────────────────
        public async Task<ApiResponse<AuthResponse>> RegisterAsync(
            RegisterRequest request, string role)
        {
            _logger.LogInformation("Registration attempt for username: {UserName}",
                request.UserName);

            if (request.Password != request.ConfirmPassword)
                return ApiResponse<AuthResponse>.Fail(
                    "Passwords do not match", 400, "ConfirmPassword");

            if (await _userManager.FindByNameAsync(request.UserName) != null)
                return ApiResponse<AuthResponse>.Fail(
                    "Username already taken", 409, "UserName");

            var phoneExists = await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
            if (phoneExists)
                return ApiResponse<AuthResponse>.Fail(
                    "Phone number already registered", 409, "PhoneNumber");

            var user = new ApplicationUser
            {
                FullName = request.FullName,
                UserName = request.UserName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var error = result.Errors.First();
                _logger.LogError("Registration failed for {UserName}: {Error}",
                    request.UserName, error.Description);
                return ApiResponse<AuthResponse>.Fail(error.Description, 400, "Identity");
            }

            await _userManager.AddToRoleAsync(user, role);

            // Generate tokens
            var token = _jwtService.GenerateToken(user, role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Save refresh token
            await _context.RefreshTokens.AddAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Role} registered successfully: {UserName}",
                role, request.UserName);

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                Token = token,
                RefreshToken = refreshToken
            }, "Registration successful");
        }

        // ── Login ────────────────────────────────────────────
        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("Login attempt: {UserNameOrPhone}",
                request.UserNameorPhone);

            var user = await _userManager.FindByNameAsync(request.UserNameorPhone)
                ?? await _userManager.Users.FirstOrDefaultAsync(
                    u => u.PhoneNumber == request.UserNameorPhone);

            if (user == null)
                return ApiResponse<AuthResponse>.Fail(
                    "Invalid credentials", 401, "UserNameOrPhone");

            if (!user.IsActive)
                return ApiResponse<AuthResponse>.Fail(
                    "Account is disabled", 403, "Account");

            var passwordValid = await _userManager.CheckPasswordAsync(
                user, request.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Invalid password for {UserName}", user.UserName);
                return ApiResponse<AuthResponse>.Fail(
                    "Invalid credentials", 401, "Password");
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

            // Generate tokens
            var token = _jwtService.GenerateToken(user, role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Save refresh token
            await _context.RefreshTokens.AddAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login successful for {UserName}", user.UserName);

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                Token = token,
                RefreshToken = refreshToken
            }, "Login successful");
        }

        // ── Forgot Password ──────────────────────────────────
        public async Task<ApiResponse<string>> ForgotPasswordAsync(
            ForgotPasswordRequest request)
        {
            _logger.LogInformation("Forgot password request for phone: {Phone}",
                request.PhoneNumber);

            var user = _userManager.Users
                .FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
                return ApiResponse<string>.Fail(
                    "No account found with this phone number", 404, "PhoneNumber");

            // Generate 6 digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

            // Save OTP to user
            user.OtpCodeHash = otpHash;
            user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
            user.OtpUsed = false;
            await _userManager.UpdateAsync(user);

            // Send OTP via SMS
            var smsSent = await _smsService.SendSmsAsync(user.PhoneNumber, otp);

            if (!smsSent)
            {
                _logger.LogError("Failed to send OTP SMS to {Phone}",
                    user.PhoneNumber);
                return ApiResponse<string>.Fail(
                    "Failed to send OTP. Please try again.", 500, "Sms");
            }

            _logger.LogInformation("OTP sent successfully to {Phone}",
                request.PhoneNumber);

            return ApiResponse<string>.Ok(
                "OTP sent successfully",
                "OTP sent to your registered phone number");
        }

        // ── Reset Password ───────────────────────────────────
        public async Task<ApiResponse<string>> ResetPasswordAsync(
            ResetPasswordRequest request)
        {
            _logger.LogInformation("Reset password attempt for phone: {Phone}",
                request.PhoneNumber);

            var user = _userManager.Users
                .FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
                return ApiResponse<string>.Fail(
                    "No account found with this phone number", 404, "PhoneNumber");

            if (user.OtpCodeHash == null || user.OtpExpiresAt == null)
                return ApiResponse<string>.Fail(
                    "No OTP requested", 400, "Otp");

            if (user.OtpUsed)
                return ApiResponse<string>.Fail(
                    "OTP already used", 400, "Otp");

            if (DateTime.UtcNow > user.OtpExpiresAt)
                return ApiResponse<string>.Fail(
                    "OTP has expired", 400, "Otp");

            if (!BCrypt.Net.BCrypt.Verify(request.Otp, user.OtpCodeHash))
                return ApiResponse<string>.Fail(
                    "Invalid OTP", 400, "Otp");

            if (request.NewPassword != request.ConfirmPassword)
                return ApiResponse<string>.Fail(
                    "Passwords do not match", 400, "ConfirmPassword");

            // Reset password via Identity
            var resetToken = await _userManager
                .GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(
                user, resetToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var error = result.Errors.First();
                return ApiResponse<string>.Fail(
                    error.Description, 400, "Password");
            }

            // Mark OTP as used
            user.OtpUsed = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password reset successful for {Phone}",
                request.PhoneNumber);

            return ApiResponse<string>.Ok(
                "Password reset successful",
                "You can now login with your new password");
        }
    }
}