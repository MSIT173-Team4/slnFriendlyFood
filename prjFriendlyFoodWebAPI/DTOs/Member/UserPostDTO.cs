namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class UserPostDTO
    {
        public int PostId { get; set; }
        public string Title { get; set; } = "";
        public int Likes { get; set; }
        public int Views { get; set; }
        public DateTime PostDate { get; set; }
    }
}
