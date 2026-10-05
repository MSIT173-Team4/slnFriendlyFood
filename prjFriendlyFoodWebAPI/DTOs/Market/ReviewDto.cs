namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class SubmitReviewsDto
    {
        public List<ReviewItemDto> Items { get; set; } = new();
    }

    public class ReviewItemDto
    {
        public int OrderDetailId { get; set; }
        public byte Rating { get; set; }        // 1～5
        public string? Comment { get; set; }    // 選填，最多 300 字
    }
}