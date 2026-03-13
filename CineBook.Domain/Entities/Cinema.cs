using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class Cinema
    {
        public Guid Id { get; set; }
        public string ManagerUserId { get; set; }
        public string CinemaName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string? CinemaLogo { get; set; }
        public string LicenseNumber { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
        public string? RejectionReason { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser Manager { get; set; }
        public ICollection<Hall> Halls { get; set; }
        public ICollection<Showtime> Showtimes { get; set; }
    }
}
