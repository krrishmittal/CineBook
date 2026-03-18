namespace CineBook.Application.DTOs.Requests
{
    public class InitiateBookingRequest
    {
        public Guid ShowtimeId { get; set; }
        public List<Guid> SeatIds { get; set; }
    }

    public class ConfirmBookingRequest
    {
        public Guid BookingId { get; set; }
    }
}