namespace daleel_e_shop.BLL.DTOs.AIChat
{
    public class ChatRequestDto
    {
        public List<ChatMessageDto> ConversationHistory { get; set; } = new();
        public string? UserMessage { get; set; }
    }
}
