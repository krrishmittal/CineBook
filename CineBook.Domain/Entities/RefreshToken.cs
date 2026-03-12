namespace CineBook.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt  { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        
        //navigation
        public ApplicationUser User { get; set; }
    }
}
