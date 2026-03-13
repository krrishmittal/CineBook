using Microsoft.AspNetCore.Identity;
namespace CineBook.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // OTP fields
        public string? OtpCodeHash { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
        public bool OtpIsUsed { get; set; } = false;

        // Navigation
        public ICollection<RefreshToken> RefreshTokens { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<UserFavourite> Favourites { get; set; }
    }
}
