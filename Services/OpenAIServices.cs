using OpenAI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OpenAI.Embeddings;

namespace MyRagChatBot.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly IConfiguration _configuration;

        public OpenAIService(HttpClient httpClient, ILogger<OpenAIService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Ensure BaseAddress is set
            InitializeHttpClient();

            _logger.LogInformation("OpenAIService initialized for OpenRouter");
        }

        private void InitializeHttpClient()
        {
            try
            {
                // Set BaseAddress if not already set
                if (_httpClient.BaseAddress == null)
                {
                    var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://openrouter.ai/api/v1/";
                    if (!baseUrl.EndsWith("/"))
                        baseUrl += "/";

                    _httpClient.BaseAddress = new Uri(baseUrl);
                    _logger.LogInformation($"HttpClient BaseAddress set to: {baseUrl}");
                }

                // Add Authorization header
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", apiKey);
                    }
                }

                // Add other required headers for OpenRouter
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MyRagChatBot/1.0");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "MyRagChatBot");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", "https://localhost:7031");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing HttpClient");
            }
        }

        public async Task<string> SimpleChat(string message)
        {
            try
            {
                _logger.LogInformation($"SimpleChat: {message}");

                var model = _configuration["OpenAI:Model"] ?? "openai/gpt-3.5-turbo";

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        // OPTION 3: Better system prompt
                        new
                        {
                            role = "system",
                            content = "You are a helpful assistant. Format your responses in a clean, readable way. " +
                                     "Follow these formatting rules:\n" +
                                     "1. DO NOT use markdown tables (| | syntax).\n" +
                                     "2. Use bullet points or numbered lists instead of tables.\n" +
                                     "3. For key-value information, use simple format: **Key:** Value\n" +
                                     "4. Code examples should use triple backticks with language: ```csharp\ncode here\n```\n" +
                                     "5. Keep paragraphs short and readable.\n" +
                                     "6. Use headings with ## or ### but keep it simple.\n" +
                                     "7. Make the response conversational and easy to understand."
                        },
                        new { role = "user", content = message }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Calling OpenRouter API with model: {model}");
                _logger.LogInformation($"BaseAddress: {_httpClient.BaseAddress}");
                _logger.LogInformation($"Full URL: {_httpClient.BaseAddress}chat/completions");

                var response = await _httpClient.PostAsync("chat/completions", content);

                _logger.LogInformation($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"OpenRouter response successful");

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var answer = choices[0].GetProperty("message")
                                               .GetProperty("content")
                                               .GetString() ?? "No response";

                        _logger.LogInformation($"Got answer: {answer}");

                        //Optional: Post-process to ensure no markdown tables
                        return CleanResponse(answer);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenRouter Error ({response.StatusCode}): {errorContent}");
                    return $"API Error: {response.StatusCode} - {errorContent}";
                }

                return "Sorry, I couldn't get a response.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SimpleChat");
                return $"Error: {ex.Message}";
            }
        }
        private string CleanResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return response;

            var cleaned = response;

            // Remove markdown headings
            cleaned = cleaned.Replace("## ", "")
                             .Replace("### ", "")
                             .Replace("# ", "");

            // Remove horizontal rules
            cleaned = cleaned.Replace("---", "")
                             .Replace("***", "");

            // Fix bullet points
            cleaned = cleaned.Replace("- **", "• ")
                             .Replace("**", "");

            // Clean tables
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"\|([^|\n]+)\|([^|\n]+)\|",
                match =>
                {
                    var key = match.Groups[1].Value.Trim();
                    var value = match.Groups[2].Value.Trim();
                    return $"• {key}: {value}";
                });

            // Remove table separators
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"\|[-|\s]+\|",
                "");

            // Remove asterisks used for bold
            cleaned = cleaned.Replace("*", "");

            // Fix spacing
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"\s+",
                " ");

            // Fix newlines
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"\n{3,}",
                "\n\n");

            return cleaned.Trim();
        }

        // For testing, return dummy embeddings
        public async Task<float[]> GetEmbedding(string text)
        {
            try
            {
                _logger.LogInformation($"Getting embedding for text (dummy): {text.Substring(0, Math.Min(50, text.Length))}...");

                await Task.Delay(100);

                // Return dummy embedding for testing
                var random = new Random();
                var embedding = new float[768];

                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] = (float)(random.NextDouble() * 2 - 1);
                }

                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding");
                return Array.Empty<float>();
            }
        }

        public async Task<string> GetChatResponse(string userQuestion, string context = "")
        {
            try
            {
                _logger.LogInformation($"GetChatResponse with context length: {context?.Length ?? 0}");

                var model = _configuration["OpenAI:Model"] ?? "openai/gpt-3.5-turbo";

                string prompt;
                if (string.IsNullOrEmpty(context))
                {
                    prompt = userQuestion;
                }
                else
                {
                    prompt = $"Context:\n{context}\n\nQuestion: {userQuestion}\n\nAnswer based on context:";
                }

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        // Same improved system prompt for RAG responses
                        new
                        {
                            role = "system",
                            content = "You are a helpful assistant that answers questions based on provided context. " +
                                     "Format your responses in a clean, readable way. " +
                                     "DO NOT use markdown tables (| | syntax). Use bullet points or simple key-value pairs instead. " +
                                     "Make sure code examples are properly formatted with triple backticks."
                        },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var answer = choices[0].GetProperty("message")
                                               .GetProperty("content")
                                               .GetString() ?? "No response";

                        // Clean the response
                        return CleanResponse(answer);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Chat API Error: {errorContent}");
                }

                return "Could not get response from AI.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChatResponse");
                return $"Error: {ex.Message}";
            }
        }

        // Dummy embedding for testing (optional)
        private async Task<float[]> GetDummyEmbedding(string text)
        {
            await Task.Delay(100);

            var random = new Random();
            var embedding = new float[1536];

            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1);
            }

            // Normalize
            var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] /= (float)magnitude;
                }
            }

            return embedding;
        }
        //////////////////// For PDF/File Upload ///////////////
        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            try
            {
                var model = _configuration["OpenAI:EmbeddingModel"]
                            ?? "openai/text-embedding-3-small";

                var requestBody = new
                {
                    model = model,
                    input = text
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Creating embedding using model: {model}");

                var response = await _httpClient.PostAsync("embeddings", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Embedding API error: {error}");
                    return Array.Empty<float>();
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var embedding = doc.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding")
                    .EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToArray();

                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating embedding");
                return Array.Empty<float>();
            }
        }
    }
}