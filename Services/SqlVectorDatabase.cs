using Microsoft.EntityFrameworkCore;
using MyRagChatBot.Data;
using MyRagChatBot.Models;

namespace MyRagChatBot.Services
{
    public class SqlVectorDatabase : IVectorDatabase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SqlVectorDatabase> _logger;

        public SqlVectorDatabase(AppDbContext context, ILogger<SqlVectorDatabase> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task StoreDocumentChunk(DocumentChunk chunk)
        {
            try
            {
                _context.DocumentChunks.Add(chunk);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Stored chunk {chunk.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing document chunk");
                throw;
            }
        }

        public async Task<List<DocumentChunk>> SearchSimilarChunks(float[] queryEmbedding, int topK = 5)
        {
            try
            {
                // Get all chunks from database
                var allChunks = await _context.DocumentChunks.ToListAsync();

                if (allChunks.Count == 0)
                    return new List<DocumentChunk>();

                // Calculate similarity for each chunk
                var chunksWithSimilarity = new List<(DocumentChunk Chunk, double Similarity)>();

                foreach (var chunk in allChunks)
                {
                    var chunkEmbedding = chunk.GetEmbedding();

                    // Only compare if embeddings have same length
                    if (chunkEmbedding.Length == queryEmbedding.Length)
                    {
                        var similarity = CalculateCosineSimilarity(queryEmbedding, chunkEmbedding);
                        chunksWithSimilarity.Add((chunk, similarity));
                    }
                }

                // Sort by similarity (highest first) and take top K
                var topChunks = chunksWithSimilarity
                    .OrderByDescending(x => x.Similarity)
                    .Take(topK)
                    .Where(x => x.Similarity > 0.7) // Threshold for relevance
                    .Select(x => x.Chunk)
                    .ToList();

                return topChunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar chunks");
                return new List<DocumentChunk>();
            }
        }

        public async Task<List<DocumentChunk>> GetAllChunks()
        {
            return await _context.DocumentChunks.ToListAsync();
        }

        public async Task ClearAllChunks()
        {
            _context.DocumentChunks.RemoveRange(_context.DocumentChunks);
            await _context.SaveChangesAsync();
        }

        // Helper method to calculate cosine similarity
        private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                return 0.0;

            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0.0;

            return dotProduct / (magnitudeA * magnitudeB);
        }
    }
}
