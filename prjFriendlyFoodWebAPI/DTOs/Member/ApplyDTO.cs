namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class ApplyDTO
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string? StoreDescription { get; set; } = string.Empty;
    }
}
