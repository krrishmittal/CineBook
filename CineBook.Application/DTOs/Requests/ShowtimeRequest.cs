namespace CineBook.Application.DTOs.Requests
{
    public class CreateShowtimeRequest
    {
        public Guid MovieId { get; set; }
        public Guid HallId { get; set; }
        public DateTime StartTime { get; set; }
        public decimal PriceStandard { get; set; }
        public decimal PricePremium { get; set; }
        public decimal PriceVIP { get; set; }
    }

    public class UpdateShowtimeRequest
    {
        public DateTime StartTime { get; set; }
        public decimal PriceStandard { get; set; }
        public decimal PricePremium { get; set; }
        public decimal PriceVIP { get; set; }
        public bool IsActive { get; set; }
    }
}