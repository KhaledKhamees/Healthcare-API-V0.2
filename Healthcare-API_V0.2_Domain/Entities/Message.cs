namespace Healthcare_API.Models
{
    public class Message
    {
        public int Id { get; private set; }
        public int ConversationId { get; private set; }
        public Conversation Conversation { get; private set; } = default!;
        public string SenderId { get; private set; } = default!;
        public string ReceiverId { get; private set; } = default!;
        public string Text { get; private set; } = default!;
        public DateTime SentAt { get; private set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; private set; }
        public DateTime? ReadAt { get; private set; }

        public Message(int conversationId, string senderId, string receiverId, string text)
        {
            ConversationId = conversationId;
            SenderId = senderId;
            ReceiverId = receiverId;
            Text = text;
            SentAt = DateTime.UtcNow;
        }

        public void MarkAsDelivered()
        {
            DeliveredAt = DateTime.UtcNow;
        }

        public void MarkAsRead()
        {
            ReadAt = DateTime.UtcNow;
        }
    }
}
