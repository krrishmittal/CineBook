using CineBook.Application.DTOs.Responses;

namespace CineBook.Application.Interfaces
{
    public interface IFavouriteService
    {
        Task<ApiResponse<bool>> ToggleFavouriteAsync(string userId, Guid movieId);
        Task<ApiResponse<List<FavouriteResponse>>> GetMyFavouritesAsync(string userId);
        Task<ApiResponse<List<Guid>>> GetFavouriteIdsAsync(string userId);
    }
}