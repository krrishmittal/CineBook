using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public string BookingReference { get; set; }
        public string UserId { get; set; }
        public Guid ShowtimeId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal TotalAmount { get; set; }
        public BookingStatus Status { get; set; }
        public bool SmsReminderSent { get; set; } = false;
        public DateTime BookedAt { get; set; }
        public bool RefundProcessed { get; set; } = false;
        public DateTime? RefundedAt { get; set; }
        public string? RefundNote { get; set; }

        //navigation
        public ApplicationUser User { get; set; }
        public Showtime Showtime { get; set; }
        public ICollection<BookingSeat> BookingSeats { get; set; }
        public Payment Payment { get; set; }
    }
}
