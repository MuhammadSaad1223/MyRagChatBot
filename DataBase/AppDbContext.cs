using Microsoft.EntityFrameworkCore;
using MyRagChatBot.Models;

namespace MyRagChatBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        //public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<DocumentChunk> DocumentChunks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DocumentChunk
            modelBuilder.Entity<DocumentChunk>()
                .Property(d => d.EmbeddingJson)
                .HasColumnType("nvarchar(max)");

            // Configure ChatMessage
            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Message)
                .HasColumnType("nvarchar(max)");

            // Configure ChatSession
            modelBuilder.Entity<ChatSession>()
                .HasMany(s => s.Messages)
                .WithOne(m => m.ChatSession)
                .HasForeignKey(m => m.SessionIntId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            modelBuilder.Entity<DocumentChunk>()
                .HasIndex(d => d.DocumentName);

            modelBuilder.Entity<ChatSession>()
                .HasIndex(s => s.SessionId)
                .IsUnique();

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => m.SessionId);

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => m.Timestamp);
        }
    }
}
