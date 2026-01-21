/*using Microsoft.AspNetCore.Components.Forms;
using MyRagChatBot.Models;

namespace MyRagChatBot.Services
{
    public class RagService
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<RagService> _logger;

        public RagService(
            IOpenAIService openAIService,
            ILogger<RagService> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
            _logger.LogInformation("RagService initialized");
        }

        // Main RAG pipeline - SIMPLIFIED
        public async Task<string> ProcessQuery(string query)
        {
            _logger.LogInformation($"🔵 ProcessQuery called: '{query}'");

            try
            {
                // Directly use SimpleChat for testing
                var response = await _openAIService.SimpleChat(query);

                _logger.LogInformation($"🟢 Response generated: {response}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in ProcessQuery");
                return $"Error: {ex.Message}";
            }
        }

        // Process document - SIMPLIFIED
        public async Task<string> ProcessDocument(IBrowserFile file)
        {
            _logger.LogInformation($"📄 ProcessDocument called: {file.Name}");

            try
            {
                await Task.Delay(1000); // Simulate processing
                return $"✅ Successfully uploaded: {file.Name} (Test mode)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document");
                return $"Error: {ex.Message}";
            }
        }
    }
}*/


using Microsoft.AspNetCore.Components.Forms;
using MyRagChatBot.Models;
using System.Text;

namespace MyRagChatBot.Services
{
    public class RagService
    {
        private readonly IOpenAIService _openAIService;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly IDocumentService _documentService;
        private readonly ILogger<RagService> _logger;

        public RagService(
            IOpenAIService openAIService,
            IVectorDatabase vectorDatabase,
            IDocumentService documentService,
            ILogger<RagService> logger)
        {
            _openAIService = openAIService;
            _vectorDatabase = vectorDatabase;
            _documentService = documentService;
            _logger = logger;
        }

        // Main RAG pipeline for processing user queries
        public async Task<string> ProcessQuery(string query)
        {
            _logger.LogInformation($"Processing query: {query}");

            try
            {
                // Step 1: Get embedding for the query text
                var queryEmbedding = await _openAIService.GetEmbedding(query);

                if (queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("Failed to get query embedding, falling back to simple chat");
                    return await _openAIService.SimpleChat(query);
                }

                // Step 2: Search the vector DB for the most similar document chunks
                var similarChunks = await _vectorDatabase.SearchSimilarChunks(queryEmbedding, 3);

                if (similarChunks.Count == 0)
                {
                    _logger.LogInformation("No relevant documents found, using simple chat");
                    return await _openAIService.SimpleChat(query);
                }

                // Step 3: Build a contextual prompt from retrieved chunks
                string context = BuildContextFromChunks(similarChunks);

                // Step 4: Query OpenAI with context and user query to get the final answer
                var answer = await _openAIService.GetChatResponse(query, context);

                _logger.LogInformation("Successfully generated RAG response");
                return answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RAG pipeline");
                return $"Error: {ex.Message}. Please try again.";
            }
        }

        // Process an uploaded document file and store embeddings in vector DB
        public async Task<string> ProcessDocument(IBrowserFile file)
        {
            try
            {
                // Extract text content from the uploaded file (PDF, TXT, etc.)
                var text = await _documentService.ExtractTextFromFile(file);

                if (string.IsNullOrEmpty(text))
                    return "Could not extract text from file.";

                // Split extracted text into manageable chunks for embeddings
                var chunks = _documentService.SplitIntoChunks(text, 800);

                int processedCount = 0;

                // For each text chunk, generate embedding and store in DB
                foreach (var chunk in chunks)
                {
                    if (string.IsNullOrWhiteSpace(chunk))
                        continue;

                    var embedding = await _openAIService.GetEmbedding(chunk);

                    if (embedding.Length == 0)
                        continue;

                    var documentChunk = new DocumentChunk
                    {
                        DocumentName = file.Name,
                        Content = chunk,
                        UploadedDate = DateTime.Now,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    };

                    documentChunk.SetEmbedding(embedding);

                    // Store this chunk + embedding in your vector DB
                    await _vectorDatabase.StoreDocumentChunk(documentChunk);
                    processedCount++;
                }

                return $"Successfully processed {processedCount} chunks from {file.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document");
                return $"Error: {ex.Message}";
            }
        }

        // Helper method to create a single context string from document chunks
        private string BuildContextFromChunks(List<DocumentChunk> chunks)
        {
            var context = new StringBuilder();
            int chunkNumber = 1;

            foreach (var chunk in chunks)
            {
                context.AppendLine($"[Document {chunkNumber} from '{chunk.DocumentName}']");
                context.AppendLine(chunk.Content);
                context.AppendLine();
                chunkNumber++;
            }

            return context.ToString().Trim();
        }

        // Retrieve all stored document chunks (optional helper)
        public async Task<List<DocumentChunk>> GetAllStoredChunks()
        {
            return await _vectorDatabase.GetAllChunks();
        }

        // Clear all documents from the vector database (optional helper)
        public async Task ClearAllDocuments()
        {
            await _vectorDatabase.ClearAllChunks();
        }
    }
}

