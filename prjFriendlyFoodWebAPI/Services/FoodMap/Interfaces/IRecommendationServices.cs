using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using System.Runtime.CompilerServices;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface IRecommendationServices
    {
        Task<List<RecommendationDTO>> GetRecommendationDTOsAsync(int shoppinglistId); 
    }
}
