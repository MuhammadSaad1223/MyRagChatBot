using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyRagChatBot.Services
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiAIService> _logger;
        private readonly IConfiguration _configuration;

        public GeminiAIService(HttpClient httpClient, ILogger<GeminiAIService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Ensure BaseAddress is set
            InitializeHttpClient();

            _logger.LogInformation("GeminiAIService initialized");
        }

        private void InitializeHttpClient()
        {
            try
            {
                // Set BaseAddress for Gemini API
                if (_httpClient.BaseAddress == null)
                {
                    var baseUrl = _configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/";
                    
                    _logger.LogInformation($"BaseUrl from config: {_configuration["Gemini:BaseUrl"]}");
                    _logger.LogInformation($"ApiKey exists: {!string.IsNullOrEmpty(_configuration["Gemini:ApiKey"])}");
                    
                    if (!baseUrl.EndsWith("/"))
                        baseUrl += "/";

                    _httpClient.BaseAddress = new Uri(baseUrl);
                    _logger.LogInformation($"HttpClient BaseAddress set to: {baseUrl}");
                }

                // Clear any existing headers
                _httpClient.DefaultRequestHeaders.Clear();

                // Add User-Agent
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MyRagChatBot/1.0");

                // Note: Gemini API uses API key as query parameter, not Authorization header
                // So we don't set Authorization header here
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

                var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var apiKey = _configuration["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Gemini API Key is missing in configuration");
                    return "Error: Gemini API Key is not configured.";
                }

                // Gemini API format (different from OpenAI)
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = message }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = 2048,
                        temperature = 0.7,
                        topP = 0.95,
                        topK = 40
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                            threshold = "BLOCK_NONE"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gemini API endpoint format
                var endpoint = $"models/{model}:generateContent?key={apiKey}";

                _logger.LogInformation($"Calling Gemini API with model: {model}");
                _logger.LogInformation($"Full URL: {_httpClient.BaseAddress}{endpoint}");

                var response = await _httpClient.PostAsync(endpoint, content);

                _logger.LogInformation($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Gemini response successful");

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    // Extract Gemini response
                    if (root.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0 &&
                        candidates[0].TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var text))
                    {
                        var answer = text.GetString() ?? "No response";
                        _logger.LogInformation($"Got answer: {answer}");
                        return CleanResponse(answer);
                    }
                    else
                    {
                        _logger.LogWarning($"Unexpected response format: {responseJson}");
                        return "Sorry, I received an unexpected response format.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini Error ({response.StatusCode}): {errorContent}");
                    return $"API Error: {response.StatusCode} - {errorContent}";
                }
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

                var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash-latest";
                var apiKey = _configuration["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return "Error: Gemini API Key is not configured.";
                }

                string prompt;
                if (string.IsNullOrEmpty(context))
                {
                    prompt = userQuestion;
                }
                else
                {
                    prompt = $"Context:\n{context}\n\nQuestion: {userQuestion}\n\nAnswer based on context:";
                }

                // Gemini API format
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = 2048,
                        temperature = 0.3,
                        topP = 0.95,
                        topK = 40
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_NONE"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gemini endpoint
                var endpoint = $"models/{model}:generateContent?key={apiKey}";
                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    // Extract Gemini response
                    if (root.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0 &&
                        candidates[0].TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var text))
                    {
                        var answer = text.GetString() ?? "No response";
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

        // For PDF/File Upload - Updated for Gemini
        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            try
            {
                var model = _configuration["Gemini:EmbeddingModel"] ?? "embedding-001";
                var apiKey = _configuration["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Gemini API Key is missing for embeddings");
                    return Array.Empty<float>();
                }

                // Gemini embedding format
                var requestBody = new
                {
                    model = $"models/{model}",
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = text }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Creating embedding using model: {model}");

                // Gemini embedding endpoint
                var endpoint = $"models/{model}:embedContent?key={apiKey}";
                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Embedding API error: {error}");
                    return Array.Empty<float>();
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                // Extract embedding from Gemini response
                if (doc.RootElement.TryGetProperty("embedding", out var embeddingObj) &&
                    embeddingObj.TryGetProperty("values", out var values))
                {
                    var embedding = values.EnumerateArray()
                                          .Select(x => x.GetSingle())
                                          .ToArray();
                    return embedding;
                }

                return Array.Empty<float>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating embedding");
                return Array.Empty<float>();
            }
        }
    }

    // Interface definition
    public interface IGeminiAIService
    {
        Task<string> SimpleChat(string message);
        Task<float[]> GetEmbedding(string text);
        Task<string> GetChatResponse(string userQuestion, string context = "");
        Task<float[]> CreateEmbeddingAsync(string text);
    }
}