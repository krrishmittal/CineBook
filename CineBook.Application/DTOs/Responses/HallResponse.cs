using CineBook.Domain.Enums;

namespace CineBook.Application.DTOs.Responses
{
    public class HallResponse
    {
        public Guid Id { get; set; }
        public Guid CinemaId { get; set; }
        public string HallName { get; set; }
        public HallType HallType { get; set; }
        public string HallTypeLabel { get; set; }
        public int TotalSeats { get; set; }
        public int StandardSeats { get; set; }
        public int PremiumSeats { get; set; }
        public int VIPSeats { get; set; }
        public bool IsActive { get; set; }
        public List<SeatResponse> Seats { get; set; }
    }

    public class SeatResponse
    {
        public Guid Id { get; set; }
        public string Row { get; set; }
        public int SeatNumber { get; set; }
        public SeatType SeatType { get; set; }
        public string SeatTypeLabel { get; set; }
        public bool IsActive { get; set; }
    }
}