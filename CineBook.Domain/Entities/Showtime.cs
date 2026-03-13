namespace CineBook.Domain.Entities
{
    public class Showtime
    {
        public Guid Id { get; set; }
        public Guid MovieId { get; set; }
        public Guid HallId { get; set; }
        public Guid CinemaId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime {  get; set; }
        public decimal PriceStandard { get; set; }
        public decimal PricePremium { get; set; }
        public decimal PriceVIP { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }= DateTime.Now;

        // navigation
        public Movie Movie { get; set; }
        public Hall Hall {  get; set; }
        public Cinema Cinema { get; set; }
        public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; }
        public ICollection<Booking>Bookings { get; set; }


    }
}
