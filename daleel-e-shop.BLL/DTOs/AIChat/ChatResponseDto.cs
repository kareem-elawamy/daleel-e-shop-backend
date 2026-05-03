namespace daleel_e_shop.BLL.DTOs.AIChat
{
    public class ChatResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? GeneratedText { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
