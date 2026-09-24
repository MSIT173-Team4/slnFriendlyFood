namespace prjFriendlyFoodWebAPI.Models
{
    public class PostQueryParameters
    {
        public string Tab { get; set; } = "latest";
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
