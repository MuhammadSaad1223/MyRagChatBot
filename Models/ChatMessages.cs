using System.ComponentModel.DataAnnotations.Schema;

namespace MyRagChatBot.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = "";
        public string Sender { get; set; } = "";  // "user" or "bot"
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Foreign key to ChatSession
        [ForeignKey("ChatSession")]
        public int? SessionIntId { get; set; }

        // Navigation property
        public virtual ChatSession? ChatSession { get; set; }

        // Helper property
        public bool IsUser => Sender == "user";

        // For RAG context
        public string? SourceType { get; set; }  // "document", "general", "error"
        public float? ConfidenceScore { get; set; }
        public int? TokensUsed { get; set; }
        public int? ResponseTimeMs { get; set; }
    }
}
