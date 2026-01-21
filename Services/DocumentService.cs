/*using Microsoft.AspNetCore.Components.Forms;
using System.Text;

namespace MyRagChatBot.Services
{
    public class DocumentService : IDocumentService
    {
        public async Task<string> ProcessTextFile(IBrowserFile file)
        {
            if (file == null || file.Size == 0)
                return string.Empty;

            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        public async Task<string> ExtractTextFromFile(IBrowserFile file)
        {
            var extension = Path.GetExtension(file.Name).ToLower();

            if (extension == ".txt")
            {
                return await ProcessTextFile(file);
            }
            else if (extension == ".pdf")
            {
                return await ExtractTextFromPdf(file);
            }
            else
            {
                return "Unsupported file type. Please upload .txt or .pdf files.";
            }
        }

        private async Task<string> ExtractTextFromPdf(IBrowserFile file)
        {
            // For now, return simple message
            // In production, use a proper PDF library like iTextSharp
            return $"PDF file '{file.Name}' uploaded. For production, implement PDF text extraction.";
        }

        public List<string> SplitIntoChunks(string text, int chunkSize = 1000)
        {
            var chunks = new List<string>();

            if (string.IsNullOrEmpty(text))
                return chunks;

            // Split by sentences first
            var sentences = text.Split(new[] { '.', '!', '?', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new StringBuilder();

            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (string.IsNullOrEmpty(trimmedSentence))
                    continue;

                if (currentChunk.Length + trimmedSentence.Length > chunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                    currentChunk.Append(trimmedSentence);
                }
                else
                {
                    if (currentChunk.Length > 0)
                    {
                        currentChunk.Append(". ");
                        currentChunk.Append(trimmedSentence);
                    }
                    else
                    {
                        currentChunk.Append(trimmedSentence);
                    }
                }
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString());

            return chunks;
        }
    }
}*/

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Components.Forms;
using MyRagChatBot.Data;
using MyRagChatBot.Models;
using System.Text;

namespace MyRagChatBot.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly AppDbContext _db;
        private readonly IGeminiAIService _geminiAi;

        public DocumentService(AppDbContext db, IGeminiAIService geminiAi)
        {
            _db = db;
            _geminiAi = geminiAi;
        }

        public async Task ProcessFileAsync(IBrowserFile file)
        {
            if (file == null || file.Size == 0)
                return;

            var text = await ExtractTextFromFile(file);
            if (string.IsNullOrWhiteSpace(text))
                return;

            var chunks = SplitIntoChunks(text);

            foreach (var chunk in chunks)
            {
                var embedding = await _geminiAi.CreateEmbeddingAsync(chunk);

                _db.DocumentChunks.Add(new DocumentChunk
                {
                    Content = chunk,
                    Embedding = embedding,
                    FileName = file.Name,
                    UploadedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<string> ProcessTextFile(IBrowserFile file)
        {
            using var stream = file.OpenReadStream(10 * 1024 * 1024);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        public async Task<string> ExtractTextFromFile(IBrowserFile file)
        {
            return Path.GetExtension(file.Name).ToLower() switch
            {
                ".txt" => await ProcessTextFile(file),
                ".pdf" => await ExtractTextFromPdf(file),
                _ => string.Empty
            };
        }

        private async Task<string> ExtractTextFromPdf(IBrowserFile file)
        {
            using var stream = file.OpenReadStream(20_000_000);
            using var reader = new PdfReader(stream);
            using var pdf = new PdfDocument(reader);

            var sb = new StringBuilder();

            for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                var page = pdf.GetPage(i);
                sb.AppendLine(PdfTextExtractor.GetTextFromPage(page));
            }

            await Task.CompletedTask;
            return sb.ToString();
        }

        public List<string> SplitIntoChunks(string text, int chunkSize = 1000)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            var sentences = text.Split(
                new[] { '.', '!', '?', ';', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            var current = new StringBuilder();

            foreach (var sentence in sentences)
            {
                var s = sentence.Trim();
                if (current.Length + s.Length > chunkSize)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }

                if (current.Length > 0)
                    current.Append(". ");

                current.Append(s);
            }

            if (current.Length > 0)
                chunks.Add(current.ToString());

            return chunks;
        }
    }
}
