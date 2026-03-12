using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class Seat
    {
        public Guid Id { get; set; }
        public Guid HallId { get; set; }
        public string Row { get; set; }
        public int SeatNumber { get; set; }
        public SeatType SeatType { get; set; }
        public bool IsActive { get; set; }

        //navigation
        public Hall Hall { get; set; }
        public ICollection<ShowtimeSeat>ShowtimeSeats { get; set; }
        public ICollection<BookingSeat>BookingSeats { get; set; }

    }
}
