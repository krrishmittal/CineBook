using CineBook.Application.DTOs.Requests;
using CineBook.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace CineBook.Application.Interfaces
{
    public interface ICinemaService
    {
        //cinema manager
        Task<ApiResponse<CinemaResponse>> RegisterCinemaAsync(string managerId, RegisterCinemaRequest request);
        Task<ApiResponse<CinemaResponse>> GetMyCinemaAsync(string managerId);

        //admin
        Task<ApiResponse<List<CinemaResponse>>> GetAllCinemasAsync(string? status);
        Task<ApiResponse<CinemaResponse>> ApproveCinemaAsync(Guid cinemaId, ApproveCinemaRequest request);

    }
}
