namespace CineBook.Domain.Entities
{
    public class UserFavourite
    {
        public string UserId { get; set; }
        public Guid MovieId { get; set; }
        public DateTime AddedAt { get; set; }
        
        //navigation 
        public ApplicationUser User { get; set; }
        public Movie Movie { get; set; }
    }
}
