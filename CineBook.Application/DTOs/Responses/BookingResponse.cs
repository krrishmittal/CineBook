using CineBook.Domain.Enums;

namespace CineBook.Application.DTOs.Responses
{
    public class BookingResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string BookingReference { get; set; }
        public string MovieTitle { get; set; }
        public string MoviePoster { get; set; }
        public string CinemaName { get; set; }
        public string HallName { get; set; }
        public DateTime ShowtimeStart { get; set; }
        public DateTime ShowtimeEnd { get; set; }
        public List<BookedSeatInfo> Seats { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal TotalAmount { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusLabel { get; set; }
        public DateTime BookedAt { get; set; }
        public bool RefundProcessed { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? RefundNote { get; set; }
    }

    public class BookedSeatInfo
    {
        public string Row { get; set; }
        public int SeatNumber { get; set; }
        public SeatType SeatType { get; set; }
        public decimal PricePaid { get; set; }
    }

    public class SeatLayoutResponse
    {
        public Guid ShowtimeId { get; set; }
        public string MovieTitle { get; set; }
        public string CinemaName { get; set; }
        public string HallName { get; set; }
        public string HallType { get; set; }
        public DateTime StartTime { get; set; }
        public decimal PriceStandard { get; set; }
        public decimal PricePremium { get; set; }
        public decimal PriceVIP { get; set; }
        public List<SeatInfo> Seats { get; set; }
    }

    public class SeatInfo
    {
        public Guid SeatId { get; set; }
        public Guid ShowtimeSeatId { get; set; }
        public string Row { get; set; }
        public int SeatNumber { get; set; }
        public SeatType SeatType { get; set; }
        public string SeatTypeLabel { get; set; }
        public string Status { get; set; } // Available, Locked, Booked
        public bool IsMyLock { get; set; }
        public decimal Price { get; set; }
    }
}