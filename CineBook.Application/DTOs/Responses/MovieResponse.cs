using CineBook.Domain.Enums;

namespace CineBook.Application.DTOs.Responses
{
    public class MovieResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }

        public string Genre { get; set; }
        public string Cast { get; set; }
        public string Director { get; set; }
        public int DurationTime { get; set; }
        public string CertificateRating { get; set; }
        public string PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public MovieStatus Status { get; set; }
        public string StatusLabel { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}