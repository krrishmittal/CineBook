namespace CineBook.Domain.Enums
{
    public enum HallType
    {
        TwoD = 0,
        ThreeD = 1,
        IMAX = 2
    }
    
    public enum SeatType
    {
        Standard = 0,
        Premium = 1,
        VIP = 2
    }

    /// <summary>
    /// 
    /// </summary>
    public enum SeatStatus
    {
        Available = 0, //
        Locket = 1, //
        Booked = 2 //
    }

    public enum MovieStatus
    {
        ComingSoon = 0,
        NowShowing = 1,
        Archived = 2
    }

    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2,
        Expired = 3
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Refunded = 3
    }

    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
