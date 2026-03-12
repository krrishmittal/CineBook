using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class Hall
    {
        public Guid Id { get; set; }
        public Guid CinemaId { get; set; }
        public string HallName { get; set; }
        public HallType HallType { get; set; }
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; }
        
        //navigation
        public Cinema Cinema { get; set; }
        public ICollection<Seat> Seats { get; set; }
        public ICollection<Showtime> Showtimes  { get; set; }

    }
}
