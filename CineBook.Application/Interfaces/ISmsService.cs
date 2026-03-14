namespace CineBook.Application.Interfaces
{
    public interface ISmsService
    {
        Task<bool>SendSmsAsync(string phoneNumber, string message);
    }
}
