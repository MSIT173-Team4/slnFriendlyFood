namespace SmartBot.Api.DTOS
{
    public class ChatResponseDto
    {
        public required string Reply { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
