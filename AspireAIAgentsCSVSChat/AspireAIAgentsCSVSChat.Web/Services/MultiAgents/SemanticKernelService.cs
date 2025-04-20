using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;
// Added Microsoft.SemanticKernel.Agents (1.4.7)
using Microsoft.SemanticKernel.Agents;
// Added Microsoft.SemanticKernel.Agents.OpenAI (1.4.7-preview)
using Microsoft.SemanticKernel.Agents.Chat;
using AspireAIAgentsCSVSChat.Web.Services.Interfaces;
using Microsoft.Extensions.AI;
using AspireAIAgentsCSVSChat.Web.Services.Configuration;
using AspireAIAgentsCSVSChat.Web.Services.Models;
using AspireAIAgentsCSVSChat.Web.Services.Factories;
// Added Microsoft.SemanticKernel.Agents.Abstractions (1.4.7-preview)
// Added Microsoft.SemanticKernel.Connectors.OpenAI (1.4.7)
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Azure.AI.OpenAI;
using Aspire.Azure.AI.OpenAI;
using Azure;
// Added Microsoft.SemanticKernel (1.4.7)

namespace AspireAIAgentsCSVSChat.Web.Services.MultiAgents
{
    public class SemanticKernelService : ISemanticKernelService, IDisposable
    {
        readonly SemanticKernelServiceSettings _settings;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger<SemanticKernelService> _logger;
        readonly Kernel _semanticKernel;
        string endpoint = "";
        string key = "";

        public SemanticKernelService(SemanticKernelServiceSettings settings)
        {
            _settings = settings;

            // Extract OpenAI settings from SemanticKernelServiceSettings
            var azureOpenAISettings = _settings.AzureOpenAISettings;

            // Initialize the kernel builder
            var builder = Kernel.CreateBuilder();

            // Configure the kernel with OpenAI settings
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: "gpt-4o-mini",
                endpoint: azureOpenAISettings.Endpoint.ToString(),
                apiKey: azureOpenAISettings.Key
            );

            // Build the kernel
            _semanticKernel = builder.Build();
        }

        public SemanticKernelService(IConfiguration configuration)
        {
            // Get endpoint and key credential from configuration

            var connectionString = configuration.GetConnectionString("openai");

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                // Assume the connection string is formatted like: "Endpoint=https://api.openai.com/;ApiKey=xxxx"
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Assume the connection string is formatted like: "Endpoint=https://api.openai.com/;ApiKey=xxxx"  
                    var parameters = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    endpoint = parameters.FirstOrDefault(p => p.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
                                             ?.Split('=')[1].ToString();
                    key = parameters.FirstOrDefault(p => p.StartsWith("Key=", StringComparison.OrdinalIgnoreCase))
                                         ?.Split('=')[1].ToString();

                    // Initialize AzureOpenAIClient
                    _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                }
            }
        }

        private readonly AzureOpenAIClient _openAIClient;

        public AzureOpenAIClient GetClient()
        {
            return _openAIClient;
        }

        public SemanticKernelService()
        {
            // Default constructor for cases where settings are not provided
            var builder = Kernel.CreateBuilder();
            _semanticKernel = builder.Build();
        }

        public async Task<List<ChatMessageContent>> GetDemoResponse(string userInput)
        {
            // Correcting the builder initialization and usage  
            var builder = Kernel.CreateBuilder();

            // Configure the kernel with OpenAI settings using _openAIClient  
            builder.AddAzureOpenAIChatCompletion("gpt-4o-mini", endpoint, key);

            // Build the kernel  
            var kernel = builder.Build();

            ChatCompletionAgent ValidationPlanningAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.ValidationPlanning)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.ValidationPlanning),
                    Kernel = kernel
                };

            ChatCompletionAgent RequirementsSpecificationAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.RequirementsSpecification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.RequirementsSpecification),
                    Kernel = kernel
                };

            ChatCompletionAgent DesignQualificationAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DesignQualification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DesignQualification),
                    Kernel = kernel
                };

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
            AgentGroupChat chat =
                new(ValidationPlanningAgent, RequirementsSpecificationAgent, DesignQualificationAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    Agents = [DesignQualificationAgent],
                                    MaximumIterations = 6,
                                }
                        }
                };
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  

            List<ChatMessageContent> messages = new List<ChatMessageContent>();
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput));

            Console.WriteLine($"# {AuthorRole.User}: '{userInput}'");

            await foreach (var content in chat.InvokeAsync())
            {
                messages.Add(content);
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
                Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
            }

            // Return list of messages from the multiagent  
            return messages;
        }

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private sealed class ApprovalTerminationStrategy : TerminationStrategy
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            // Terminate when the final message contains the term "approve"
            protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
                => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public async Task<ChatMessageContent> GetResponse(ChatMessage userMessage, List<ChatMessage> messageHistory)
        {
            // Create a new kernel instance  
            var kernel = Kernel.CreateBuilder().Build();

            // Add the user message to the message history  
            messageHistory.Add(userMessage);

            // Create a chat completion agent  
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            if (chatCompletionService == null)
            {
                throw new InvalidOperationException("ChatCompletion service is not available in the kernel.");
            }

            // Convert message history to a format compatible with the chat completion service  
            var chatHistory = messageHistory.Select(msg => new ChatMessage
            {
                
                AuthorName = msg.AuthorName,
                Contents = msg.Contents
            }).ToList();

            // Invoke the chat completion service  
            ChatHistory history = new ChatHistory();
            history.AddUserMessage("Hello, how are you?");

            var response = await chatCompletionService.GetChatMessageContentAsync(
                history,
                kernel: kernel
            );

            return response;
        }

        public async Task<string> Summarize(string userPrompt)
        {
            try
            {
                // Use an AI function to summarize the text in 2 words
                var summarizeFunction = _semanticKernel.CreateFunctionFromPrompt(
                    "Summarize the following text into exactly two words:\n\n{{$input}}",
                    executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 10 }
                );

                // Invoke the function
                var summary = await _semanticKernel.InvokeAsync(summarizeFunction, new() { ["input"] = userPrompt });

                return summary.GetValue<string>() ?? "No summary generated";
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error when getting response: {ErrorMessage}", ex.ToString());
                return string.Empty;
            }
        }

        public async Task<float[]> GenerateEmbedding(string text)
        {
            // Suppress the diagnostic warning SKEXP0001 for the usage of ITextEmbeddingGenerationService.  
#pragma warning disable SKEXP0001
            var embeddingModel = _semanticKernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();

            var embedding = await embeddingModel.GenerateEmbeddingAsync(text);

            // Convert ReadOnlyMemory<float> to IList<float>
            return embedding.ToArray();
        }

        public void Dispose()
        {
            // Dispose of any resources if necessary
            //_logger.LogInformation("Disposing the Semantic Kernel service...");
        }
    }

}
