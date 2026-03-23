using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IBookingService
    {
        Task<ApiResponse<SeatLayoutResponse>> GetSeatLayoutAsync(Guid showtimeId, string userId);
        Task<ApiResponse<BookingResponse>> InitiateBookingAsync(string userId, InitiateBookingRequest request);
        Task<ApiResponse<BookingResponse>> ConfirmBookingAsync(string userId, ConfirmBookingRequest request);   
        Task<ApiResponse<List<BookingResponse>>> GetMyBookingsAsync(string userId);
        Task<ApiResponse<List<BookingResponse>>> GetCinemaBookingsAsync(string managerId, string? date);
        Task<ApiResponse<List<BookingResponse>>> GetCancelledBookingsAsync(string managerId);
        Task<ApiResponse<BookingResponse>> GetBookingByIdAsync(Guid bookingId, string userId);
        Task<ApiResponse<string>> ProcessRefundAsync(Guid bookingId, string managerId, string? note);
        Task<ApiResponse<string>> SavePaymentAsync(Guid bookingId, string stripePaymentIntentId, decimal amount);
        Task<ApiResponse<string>> ReleaseLockedSeatsAsync(Guid bookingId, string userId);
        Task<ApiResponse<string>> CancelBookingAsync(Guid bookingId, string userId);

        Task SendBookingConfirmationAsync(Guid bookingId);
    }
}