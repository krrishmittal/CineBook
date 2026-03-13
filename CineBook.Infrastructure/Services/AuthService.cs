using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CineBook.Infrastructure.Services
{
    public class AuthService:IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly AppDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        public AuthService(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser>signInManager, AppDbContext context, ILogger<AuthService> logger,IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager= signInManager;
            _logger= logger;
            _context = context;
            _jwtService = jwtService;
        }
        public async Task<ApiResponse<AuthResponse>>RegisterAsync(RegisterRequest request,string role)
        {
            _logger.LogInformation("Registeration attempt for username:{UserName}",request.UserName);
            if (request.Password != request.ConfirmPassword)
            {
                return ApiResponse<AuthResponse>.Fail("Passwords do not match", 400, "ConfirmPassword");
            }
            if(await _userManager.FindByNameAsync(request.UserName) != null)
            {
                return ApiResponse<AuthResponse>.Fail("Username already taken", 409, "UserName");
            }
            var phoneExists = _userManager.Users.Any(u => u.PhoneNumber == request.PhoneNumber);
            if (phoneExists)
            {
                return ApiResponse<AuthResponse>.Fail("Phone number already registered", 409, "UserName");
            }

            var user = new ApplicationUser
            {
                FullName = request.FullName,
                UserName = request.UserName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt= DateTime.UtcNow

            };
            var result=await _userManager.CreateAsync(user,request.Password);
            if (!result.Succeeded)
            {
                var error = result.Errors.First();
                _logger.LogInformation("Registeration failed for {username}:{errors}", request.UserName, error.Description);
                return ApiResponse<AuthResponse>.Fail(error.Description, 400, "Identity");
            }
            var token = _jwtService.GenerateToken(user, role);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();
            await _userManager.AddToRoleAsync(user, role);
            _logger.LogInformation("{Role} registered successfully: {UserName}",
                role, request.UserName);
            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                PhoneNumber = request.PhoneNumber,
                Role = role,
                Token = token,
                RefreshToken = refreshToken
            },"Registeration SuccessFull");
        }
        public async Task<ApiResponse<AuthResponse>>LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("Login attemp for username:{username}", request.UserNameorPhone);
            var user=await _userManager.FindByNameAsync(request.UserNameorPhone)??_userManager.Users.FirstOrDefault(u=>u.PhoneNumber==request.UserNameorPhone);

            if(user == null)
                return ApiResponse<AuthResponse>.Fail(
                    "Invalid credentials", 401, "UserNameOrPhone");

            if (!user.IsActive)
                return ApiResponse<AuthResponse>.Fail(
                    "Account is disabled", 403, "Account");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Invalid password for {UserName}", user.UserName);
                return ApiResponse<AuthResponse>.Fail(
                    "Invalid credentials", 401, "Password");
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";
            var token = _jwtService.GenerateToken(user, role);
            var refreshToken=_jwtService.GenerateRefreshToken();
            await _context.RefreshTokens.AddAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked =false
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation("Login Successful for {UserName}",user.UserName);
            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                UserId=user.Id,
                FullName=user.FullName,
                UserName=user.UserName,
                PhoneNumber=user.PhoneNumber,
                Role=role,
                Token=token,
                RefreshToken=refreshToken
            },"Login SuccessFul");
        }

        
    }
}
