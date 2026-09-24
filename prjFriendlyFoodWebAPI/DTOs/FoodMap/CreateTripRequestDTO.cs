namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class CreateTripRequestDTO
    {
        
            public string FTripName { get; set; } = string.Empty;

            public List<CreateTripPlacesRequestDTO> Places { get; set; } = [];
        
    }
}
