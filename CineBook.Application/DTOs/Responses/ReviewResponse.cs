namespace CineBook.Application.DTOs.Responses
{
    public class ReviewResponse
    {
        public Guid Id { get; set; }
        public Guid MovieId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsVerifiedBooking { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMyReview { get; set; }
    }
}