namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class UserRecipeStatDTO
    {
        public List<UserRecipeDTO> Recipes { get; set; } = new();
        public int TotalViews { get; set; }
        public int TotalLike { get; set; }
    }
}
