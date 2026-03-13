namespace CineBook.Domain.Entities
{
    public class Review
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid MovieId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public bool IsVerifiedBooking { get; set; }
        public DateTime CreatedAt { get; set; }

        //navigation
        public ApplicationUser User {  get; set; }
        public Movie Movie { get; set; }
    }
}
