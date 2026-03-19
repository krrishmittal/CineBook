namespace CineBook.Application.DTOs.Requests
{
    public class CreateCheckoutSessionRequest
    {
        public Guid BookingId { get; set; }
    }

    public class ConfirmPaymentRequest
    {
        public Guid BookingId { get; set; }
        public string SessionId { get; set; }
    }
}