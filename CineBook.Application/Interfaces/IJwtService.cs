using CineBook.Domain.Entities;

namespace CineBook.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user, string role);
        string GenerateRefreshToken();
    }
}
