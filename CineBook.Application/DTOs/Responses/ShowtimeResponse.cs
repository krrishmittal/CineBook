namespace CineBook.Application.DTOs.Responses
{
    public class ShowtimeResponse
    {
        public Guid Id { get; set; }
        public Guid MovieId { get; set; }
        public string MovieTitle { get; set; }
        public string MoviePoster { get; set; }
        public int MovieDurationTime { get; set; }
        public string MovieLanguage { get; set; }
        public Guid HallId { get; set; }
        public string HallName { get; set; }
        public string HallType { get; set; }
        public Guid CinemaId { get; set; }
        public string CinemaName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal PriceStandard { get; set; }
        public decimal PricePremium { get; set; }
        public decimal PriceVIP { get; set; }
        public bool IsActive { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}