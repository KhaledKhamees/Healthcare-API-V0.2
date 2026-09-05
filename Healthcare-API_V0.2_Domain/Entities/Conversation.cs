namespace Healthcare_API.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string UserAId { get; set; } = default!;
        public string UserBId { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
