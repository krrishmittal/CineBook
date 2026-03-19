namespace CineBook.Application.DTOs.Requests
{
    public class CreateReviewRequest
    {
        public Guid MovieId { get; set; }
        public int Rating { get; set; }  
        public string? Comment { get; set; }
    }
}