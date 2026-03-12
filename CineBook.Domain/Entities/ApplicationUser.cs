using Microsoft.AspNetCore.Identity;
namespace CineBook.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime CreatedAt {  get; set; }
        public bool IsActive { get; set; } = true;
        public string? OtpCodeHash { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
        public bool OtpUsed { get; set; }=false;

        //navigation
        public ICollection<RefreshToken> RefreshTokens { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<UserFavourite> Favourites { get; set; }
    }
}
