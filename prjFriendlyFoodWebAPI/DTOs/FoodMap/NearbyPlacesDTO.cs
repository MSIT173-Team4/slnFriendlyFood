using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class NearbyPlacesDTO
    {
        
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        [Range(0.1,3.0,ErrorMessage ="距離範圍內必須在0.1到3.0公里之間")]
        public decimal Radius { get; set; } // in kilometers
    }
}
