namespace CineBook.Domain.Enums
{
    public enum HallType
    {
        TwoD,
        ThreeD,
        IMAX
    }
    
    public enum SeatType
    {
        Standard,
        Premium,
        VIP
    }

    public enum SeatStatus
    {
        Available,
        Locket,
        Booked
    }

    public enum MovieStatus
    {
        ComingSoon,
        NowShowing,
        Archived
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Expired
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
