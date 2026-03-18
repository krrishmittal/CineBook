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
        Task<ApiResponse<string>> CancelBookingAsync(Guid bookingId, string userId);
    }
}