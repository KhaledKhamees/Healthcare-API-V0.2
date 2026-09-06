namespace Healthcare_API.Models
{
    public class Conversation
    {
        public int Id { get; private set; }
        public string UserAId { get; private set; } = default!;
        public string UserBId { get; private set; } = default!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public ICollection<Message> Messages { get; private set; } = new List<Message>();

        public Conversation(string userAId, string userBId)
        {
            UserAId = userAId;
            UserBId = userBId;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddMessage(Message message)
        {
            Messages.Add(message);
        }
    }
}
