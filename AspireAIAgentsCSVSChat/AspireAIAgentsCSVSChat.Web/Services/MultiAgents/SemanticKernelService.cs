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
using Microsoft.IdentityModel.Abstractions;
// Added Microsoft.SemanticKernel.Memory (alpha-1.4.7)
using Microsoft.SemanticKernel.Memory;
using System.ComponentModel;
using System.Text.Json;
// Added Microsoft.SemanticKernel.Plugins.Memory (alpha-1.4.7)
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
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            builder.AddAzureOpenAITextEmbeddingGeneration("text-embedding-3-small", endpoint, key);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Build the kernel  
            var kernel = builder.Build();

            // Configure FunctionInvocationFilter
            kernel.AutoFunctionInvocationFilters.Add(new AutoFunctionInvocationFilter(_logger));

            // get the embeddings generator service
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Create a text memory store and populate it with sample data
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            //var embeddingGeneration = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            VolatileMemoryStore memoryStore = new();
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            SemanticTextMemory textMemory = new(memoryStore, embeddingGenerator);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            string collectionName = "SemanticKernel";

            await PopulateMemoryAsync(collectionName, textMemory);

            // Add the text memory plugin to the kernel
            MemoryPlugin memoryPlugin = new(collectionName, textMemory);
            kernel.Plugins.AddFromObject(memoryPlugin, "Memory");

            // Configure the agents

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

            ChatCompletionAgent InstallationQualityOPAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DesignQualification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DesignQualification),
                    Kernel = kernel
                };

            ChatCompletionAgent DocumentationTrainingAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DesignQualification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DesignQualification),
                    Kernel = kernel
                };

            ChatCompletionAgent ChangeManagementAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DesignQualification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DesignQualification),
                    Kernel = kernel
                };

            ChatCompletionAgent OngoingReviewAgent =
                new()
                {
                    Instructions = $"""{SystemPromptFactory.GetAgentPrompts(AgentType.DesignQualification)}""",
                    Name = SystemPromptFactory.GetAgentName(AgentType.DesignQualification),
                    Kernel = kernel
                };

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.  
            AgentGroupChat chat =
                new(ValidationPlanningAgent, RequirementsSpecificationAgent, DesignQualificationAgent, InstallationQualityOPAgent, DocumentationTrainingAgent, ChangeManagementAgent, OngoingReviewAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    Agents = [OngoingReviewAgent],
                                    MaximumIterations = 10,
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

            // Invoke chat prompt with auto invocation of functions enabled
            var executionSettings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            var chatPrompt =
            """
            <message role="user">What is Semantic Kernel?</message>
        """;

            var response = await kernel.InvokePromptAsync(chatPrompt, new(executionSettings));

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

        /// <summary>
        /// Utility to populate a text memory store with some sample data.
        /// </summary>
        private static async Task PopulateMemoryAsync(string collection, SemanticTextMemory textMemory)
        {
            string[] entries =
            [
                "Semantic Kernel is a lightweight, open-source development kit that lets you easily build AI agents and integrate the latest AI models into your C#, Python, or Java codebase. It serves as an efficient middleware that enables rapid delivery of enterprise-grade solutions.",
                "Semantic Kernel is a new AI SDK, and a simple and yet powerful programming model that lets you add large language capabilities to your app in just a matter of minutes. It uses natural language prompting to create and execute semantic kernel AI tasks across multiple languages and platforms.",
                "In this guide, you learned how to quickly get started with Semantic Kernel by building a simple AI agent that can interact with an AI service and run your code. To see more examples and learn how to build more complex AI agents, check out our in-depth samples.",
                "The Semantic Kernel extension for Visual Studio Code makes it easy to design and test semantic functions.The extension provides an interface for designing semantic functions and allows you to test them with the push of a button with your existing models and data.",
                "The kernel is the central component of Semantic Kernel.At its simplest, the kernel is a Dependency Injection container that manages all of the services and plugins necessary to run your AI application."
            ];
            foreach (var entry in entries)
            {
                await textMemory.SaveInformationAsync(
                    collection: collection,
                    text: entry,
                    id: Guid.NewGuid().ToString());
            }
        }

        /// <summary>
        /// Plugin that provides a function to retrieve useful information from the memory.
        /// </summary>
        private sealed class MemoryPlugin(string collection, ISemanticTextMemory memory)
        {
            [KernelFunction]
            [Description("Retrieve useful information to help answer a question.")]
            public async Task<string> GetUsefulInformationAsync(
                [Description("The question being asked")] string question)
            {
                List<MemoryQueryResult> memories = await memory
                    .SearchAsync(collection, question)
                    .ToListAsync()
                    .ConfigureAwait(false);

                return JsonSerializer.Serialize(memories.Select(x => x.Metadata.Text));
            }
        }

        /// <summary>
        /// Implementation of <see cref="IFunctionInvocationFilter"/> that logs the function invocation.
        /// </summary>
        public class AutoFunctionInvocationFilter(ILogger logger) : IAutoFunctionInvocationFilter
        {
            private readonly ILogger _logger = logger;

            public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
            {
                // Example: get function information
                var functionName = context.Function.Name;

                // Example: get chat history
                var chatHistory = context.ChatHistory;

                // Example: get information about all functions which will be invoked
                var functionCalls = Microsoft.SemanticKernel.FunctionCallContent.GetFunctionCalls(context.ChatHistory.Last());

                // Example: get request sequence index
                //this._logger.LogDebug("Request sequence index: {RequestSequenceIndex}", context.RequestSequenceIndex);

                // Example: get function sequence index
                //this._logger.LogDebug("Function sequence index: {FunctionSequenceIndex}", context.FunctionSequenceIndex);

                // Example: get total number of functions which will be called
                //this._logger.LogDebug("Total number of functions: {FunctionCount}", context.FunctionCount);

                // Calling next filter in pipeline or function itself.
                // By skipping this call, next filters and function won't be invoked, and function call loop will proceed to the next function.
                await next(context);

                // Example: get function result
                var result = context.Result;

                // Example: override function result value
                context.Result = new FunctionResult(context.Result, "Result from auto function invocation filter");

                // Example: Terminate function invocation
                context.Terminate = true;
            }
        }
    }
}
