using CineBook.Application.DTOs.Responses;
using CineBook.Application.Entities;
using CineBook.Application.Interfaces;
using CineBook.Infrastructure.Persistence;

namespace CineBook.Infrastructure.Services
{
    public class ReviewService: IReviewService
    {
        private readonly AppDbContext _context;
        public ReviewService (AppDbContext context)
        {
            _context = context;
        }

        //public async Task<ApiResponse<ReviewResponse>>

    }
}
