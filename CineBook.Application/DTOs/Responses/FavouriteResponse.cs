namespace CineBook.Application.DTOs.Responses
{
    public class FavouriteResponse
    {
        public Guid MovieId { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public string Genre { get; set; }
        public string Language { get; set; }
        public int DurationTime { get; set; }
        public string CertificateRating { get; set; }
        public string Status { get; set; }
        public DateTime AddedAt { get; set; }
    }
}