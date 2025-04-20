using AspireAIAgentsCSVSChat.Web.Services.Configuration;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OpenAI;

namespace AspireAIAgentsCSVSChat.Web.Services.MultiAgents
{
    public class MultiAgentApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AzureOpenAIClient _openAIClient;
        private readonly SemanticKernelServiceSettings _semanticKernelServiceSettings;
        private readonly IConfiguration _configuration;

        public MultiAgentApiClient(HttpClient httpClient, AzureOpenAIClient openAIClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _openAIClient = openAIClient;
            _configuration = configuration;
        }

        public MultiAgentApiClient(HttpClient httpClient, AzureOpenAIClient openAIClient, SemanticKernelServiceSettings semanticKernelServiceSettings)
        {
            _httpClient = httpClient;
            _openAIClient = openAIClient;
            _semanticKernelServiceSettings = semanticKernelServiceSettings;
        }


        public async Task<string> GetMultiAgentResponseAsync(string userInput)
        {
            // Assuming a configuration object is required, pass it from _semanticKernelServiceSettings.
            //if (_semanticKernelServiceSettings == null)
            //{
            //    throw new InvalidOperationException("SemanticKernelServiceSettings is not initialized.");
            //}

            SemanticKernelService semanticKernelService = new SemanticKernelService(_configuration);
            List<ChatMessageContent> outputMessage = await semanticKernelService.GetDemoResponse(userInput);

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
