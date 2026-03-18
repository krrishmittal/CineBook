using CineBook.Domain.Enums;

namespace CineBook.Application.DTOs.Requests
{
    public class CreateHallRequest
    {
        public string HallName { get; set; }
        public HallType HallType { get; set; }
        public int Rows { get; set; }
        public int SeatsPerRow { get; set; }
        public int StandardRows { get; set; }
        public int PremiumRows { get; set; }
        public List<CustomSeatRequest>? CustomSeats { get; set; }
    }

    public class CustomSeatRequest
    {
        public string Row { get; set; }
        public int SeatNumber { get; set; }
        public int SeatType { get; set; } // 0=Standard, 1=Premium, 2=VIP
    }

    public class UpdateHallRequest
    {
        public string HallName { get; set; }
        public HallType HallType { get; set; }
        public bool IsActive; 
    }
}