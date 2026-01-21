using System.ComponentModel.DataAnnotations;

namespace MyRagChatBot.Models
{
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastActivity { get; set; } = DateTime.Now;

        // Additional info
        public string? UserName { get; set; }
        public string? IPAddress { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property (one-to-many)
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
