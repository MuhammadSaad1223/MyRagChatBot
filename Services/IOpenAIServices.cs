namespace MyRagChatBot.Services
{
    public interface IOpenAIService
    {
        Task<string> GetChatResponse(string prompt, string context = "");
        Task<float[]> GetEmbedding(string text);
        Task<string> SimpleChat(string message);
    }
}
