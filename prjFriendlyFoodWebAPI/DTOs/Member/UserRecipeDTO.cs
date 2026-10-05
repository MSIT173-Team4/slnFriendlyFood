namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class UserRecipeDTO
    {
        public int RecipeId { get; set; }
        public string Title { get; set; } = "";
        public string? CoverImageUrl { get; set; }
        public int Views { get; set; }
        public int Likes { get; set; }
        public int Favorites { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
