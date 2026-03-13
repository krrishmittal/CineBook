using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class ShowtimeSeat
    {
        public Guid Id { get; set; }
        public Guid ShowtimeId { get; set; }
        public Guid SeatId {  get; set; }
        public SeatStatus Status { get; set; }=SeatStatus.Available;
        public string? LockedByUserId { get; set; }
        public DateTime? LockedAt { get; set; }

        //navigation
        public Showtime Showtime {  get; set; }
        public Seat Seat { get; set; }
        public ApplicationUser? LockedByUser { get; set; }
    }
}
