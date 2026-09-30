namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class UserProfileDTO
    {
        public string? Lastname { get; set; }
        public string? Firstname { get; set; }
        public string Username{ get; set; }
        public string Email{ get; set; }
        public string Phone{ get; set; }
        public string IdNum { get; set; }
        public string Address { get; set; }
        public string Image { get; set; }
        public string CreateTime { get; set; }
    }
}
