using CineBook.Domain.Enums;

namespace CineBook.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public string StripePaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        
        //navigation 
        public Booking Booking { get; set; }
    }
}
