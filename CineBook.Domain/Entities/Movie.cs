using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class Movie
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PosterUrl {  get; set; }
        public string TrailerUrl { get; set; }
        public int DurationTime { get; set; }
        public string Language { get; set; }
        public string Genre { get; set; }
        public string Cast { get; set; }
        public string Director { get; set; }
        public string CertificateRating { get; set;}
        public DateTime ReleaseDate { get; set; }
        public MovieStatus Status { get; set; } = MovieStatus.ComingSoon;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt   { get; set; }
        
        //navigation 
        public ICollection<Showtime> Showtimes { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<UserFavourite> Favourites { get; set; }

    }
}
