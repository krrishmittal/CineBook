namespace CineBook.Application.Interfaces
{
    public interface ITicketService
    {
        Task<byte[]> GenerateTicketPdfAsync(Guid bookingId, string userId);
    }
}