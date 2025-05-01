using AspireAIAgentsCSVSChat.Web.Services.Configuration;
using AspireAIAgentsCSVSChat.Web.Services.Interfaces;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OpenAI;

namespace AspireAIAgentsCSVSChat.Web.Services.MultiAgents
{
    public class MultiAgentApiClient
    {
        private readonly Kernel _semanticKernel;
        private readonly HttpClient _httpClient;
        private readonly AzureOpenAIClient _openAIClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MultiAgentService> _logger;

        public MultiAgentApiClient(Kernel semanticKernel, HttpClient httpClient, AzureOpenAIClient openAIClient, IConfiguration configuration, ILogger<MultiAgentService> logger)
        {
            _semanticKernel = semanticKernel;
            _httpClient = httpClient;
            _openAIClient = openAIClient;
            _configuration = configuration;
            _logger = logger;
        }

        public MultiAgentApiClient(Kernel semanticKernel, HttpClient httpClient, AzureOpenAIClient openAIClient, ILogger<MultiAgentService> logger)
        {
            _semanticKernel = semanticKernel;
            _httpClient = httpClient;
            _openAIClient = openAIClient;
            _logger = logger;
        }


        public async Task<string> GetMultiAgentResponseAsync(string userInput)
        {
            // Assuming a configuration object is required.
            if (_configuration == null)
            {
                throw new InvalidOperationException("_configuration is not initialized.");
            }

            MultiAgentService semanticKernelService = new MultiAgentService(_semanticKernel, _configuration, _logger);
            List<ChatMessageContent> outputMessage = await semanticKernelService.GetInitialResponse(userInput);

            string outputString = "";

            foreach (ChatMessageContent singleOutputMessage in outputMessage)
            {
                Console.WriteLine($"Role: {singleOutputMessage.Role}, Content: {singleOutputMessage.Content}");
                outputString += singleOutputMessage.Content;
            }

            return outputString;
        }
    }
}
