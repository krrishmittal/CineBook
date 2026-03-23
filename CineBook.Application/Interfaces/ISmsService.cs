public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message);
    Task<bool> SendWhatsAppAsync(string phoneNumber, string message);
    Task<bool> SendWhatsAppWithMediaAsync(string phoneNumber, string message, string mediaUrl); 
}