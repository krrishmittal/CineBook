using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class BookingSeat
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public Guid SeatId { get; set; }
        public SeatType SeatType { get; set; }
        public decimal PricePaid { get; set; }

        //navigation
        public Booking Booking { get; set; }
        public Seat Seat { get; set; }
    }
}
