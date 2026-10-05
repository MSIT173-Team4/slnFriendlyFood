namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class UserPostStatDTO
    {
        public List<UserPostDTO> Posts { get; set; } = new();

        public int TotalLikes { get; set; }

        public int TotalViews { get; set; }
    }
}
