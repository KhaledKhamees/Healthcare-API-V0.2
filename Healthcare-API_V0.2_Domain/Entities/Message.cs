namespace Healthcare_API.Models
{
    public class Message
    {
        public int Id { get; set; } 
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = default!;
        public string SenderId { get; set; } = default!;
        public string ReceiverId { get; set; } = default!;
        public string Text { get; set; } = default!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
