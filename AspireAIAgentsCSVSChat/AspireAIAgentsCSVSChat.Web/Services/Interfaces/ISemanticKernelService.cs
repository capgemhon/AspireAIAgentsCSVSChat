using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace AspireAIAgentsCSVSChat.Web.Services.Interfaces
{
    internal interface ISemanticKernelService
    {
        //Task<Tuple<List<Message>, List<DebugLog>>> GetResponse(Message userMessage, List<Message> messageHistory, string tenantId, string userId);

        Task<List<ChatMessageContent>> GetDemoResponse(string userInput);
        Task<ChatMessageContent> GetResponse(ChatMessage userMessage, List<ChatMessage> messageHistory);
        Task<string> Summarize(string userPrompt);
        Task<float[]> GenerateEmbedding(string text);
    }
}
