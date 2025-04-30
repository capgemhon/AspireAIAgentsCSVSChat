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

        public SemanticKernelService(Kernel semanticKernel, IConfiguration configuration, ILogger<SemanticKernelService> logger)
        {
            _semanticKernel = semanticKernel;
            _logger = logger;
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
            // Setting the multiagents name and usage  
            const string firststage = "ValidationPlanning";
            const string secondstage = "RequirementsSpecification";
            const string thirdstage = "OngoingReview";

            // Build the kernel  
            var kernel = _semanticKernel;

            ChatCompletionAgent ValidationPlanningAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.ValidationPlanning)}""",
                    Name = firststage,
                    Kernel = kernel
                };

            ChatCompletionAgent RequirementsSpecificationAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.RequirementsSpecification)}""",
                    Name = secondstage,
                    Kernel = kernel
                };

            ChatCompletionAgent DesignQualificationAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DesignQualification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DesignQualification),
                    Kernel = kernel
                };

            ChatCompletionAgent InstallationQualityOPAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.InstallationQualityOP)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.InstallationQualityOP),
                    Kernel = kernel
                };

            ChatCompletionAgent DocumentationTrainingAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DocumentationTraining)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DocumentationTraining),
                    Kernel = kernel
                };

            ChatCompletionAgent ChangeManagementAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.ChangeManagement)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.ChangeManagement),
                    Kernel = kernel
                };

            ChatCompletionAgent OngoingReviewAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.OngoingReview)}""",
                    Name = thirdstage,
                    Kernel = kernel
                };

            KernelFunction selectionFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
                State only the name of the participant to take the next turn.

                Choose only from these participants:
                - {{{firststage}}}
                - {{{secondstage}}}
                - {{{thirdstage}}}

                Always follow these two when selecting the next participant:
                1) After user input, it is {{{firststage}}}'s turn.
                2) After {{{firststage}}}'s replies, it's {{{secondstage}}}'s turn to generate plan for the specification.

                3) Finally, it's {{{thirdstage}}} turn to review and approve the plan.
                4) If the plan is approved, the conversation ends.
                5) If the plan isn't approved, it's {{{firststage}}} turn again.

                History:
                {{$history}}
                """
                );

            KernelFunction terminateFunction = KernelFunctionFactory.CreateFromPrompt($"""{SystemPromptFactory.GetAgentPrompts(AgentType.TerminationStrategy)}""");

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
           AgentGroupChat chat =
                new(ValidationPlanningAgent, RequirementsSpecificationAgent, OngoingReviewAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, kernel)
                            {
                                Agents = [OngoingReviewAgent],
                                ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                                HistoryVariableName = "history",
                                MaximumIterations = 10
                            },
                            SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                            {
                                AgentsVariableName = "agents",
                                HistoryVariableName = "history"
                            }
                        }
                };
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  

            List<ChatMessageContent> messages = new List<ChatMessageContent>();
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput));

            Console.WriteLine($"# {AuthorRole.User}: '{userInput}'");

            await foreach (var content in chat.InvokeAsync())
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                messages.Add(new ChatMessageContent
                {
                    Role = content.Role,
                    AuthorName = content.AuthorName ?? "*",
                    Content = content.Content
                });
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
                _logger.LogInformation($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
                Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
            }

            // Return list of messages from the multiagent  
            return messages.ToList();
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
