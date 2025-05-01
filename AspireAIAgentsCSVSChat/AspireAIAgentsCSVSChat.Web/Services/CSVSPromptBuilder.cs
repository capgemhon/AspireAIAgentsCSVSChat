using Microsoft.Extensions.VectorData;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Prompts;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel;
using Microsoft.KernelMemory.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AspireAIAgentsCSVSChat.Web.Services
{
    public class CSVSPromptBuilder : IPromptProvider
    {
        private const string VerificationPrompt = """
                                              Facts:
                                              {{$facts}}
                                              ======
                                              Given only the facts above, verify the fact below.
                                              If you have sufficient information to verify, reply only with 'TRUE', nothing else.
                                              If you have sufficient information to deny, reply only with 'FALSE', nothing else.
                                              If you don't have sufficient information, reply with 'NEED MORE INFO'.
                                              User: {{$input}}
                                              Verification:
                                              """;

#pragma warning disable KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly EmbeddedPromptProvider _fallbackProvider = new();
#pragma warning restore KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        public string ReadPrompt(string promptName)
        {
            switch (promptName)
            {
                case Constants.PromptNamesAnswerWithFacts:
                    return VerificationPrompt;

                default:
                    // Fall back to the default
                    return _fallbackProvider.ReadPrompt(promptName);
            }
        }

        /// <summary>  
        /// Incorporate the VerificationPrompt into an agent for processing user input.  
        /// </summary>  
        public async Task<string> UseVerificationPromptAsync(IServiceProvider serviceProvider, string userInput, string facts)
        {
            // Get the required text embedding generation service.  
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var textEmbeddingGenerationService = serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Replace placeholders in the VerificationPrompt with actual values.  
            var prompt = VerificationPrompt
                .Replace("{{$facts}}", facts)
                .Replace("{{$input}}", userInput);

            // Generate an embedding for the prompt.  
            var promptEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(prompt);

            // Simulate processing the prompt with an AI agent (this would typically involve calling an AI model).  
            // For now, we return the prompt as a placeholder for the agent's response.  
            return await Task.FromResult(prompt);
        }

        //public async Task<string> UseVerificationPromptWithSemanticKernelAsync(IServiceProvider serviceProvider, string userInput, string facts, Kernel kernel)
        //{
        //    // Replace placeholders in the VerificationPrompt with actual values.  
        //    var prompt = VerificationPrompt
        //        .Replace("{{$facts}}", facts)
        //        .Replace("{{$input}}", userInput);

        //    // Create a semantic function using the kernel and the prompt.  
        //    var semanticFunction = kernel.(prompt);

        //    // Invoke the semantic function with the user input.  
        //    var result = await semanticFunction.InvokeAsync(userInput);

        //    // Return the result from the semantic kernel.  
        //    return result.Result;
        //}
    }

    /// <summary>
    /// Sample model class that represents a glossary entry.
    /// </summary>
    /// <remarks>
    /// Note that each property is decorated with an attribute that specifies how the property should be treated by the vector store.
    /// This allows us to create a collection in the vector store and upsert and retrieve instances of this class without any further configuration.
    /// </remarks>
    internal sealed class Glossary
    {
        [VectorStoreRecordKey]
        public string Key { get; set; }

        [VectorStoreRecordData(IsFilterable = true)]
        public string Category { get; set; }

        [VectorStoreRecordData]
        public string Term { get; set; }

        [VectorStoreRecordData]
        public string Definition { get; set; }

        [VectorStoreRecordVector(Dimensions: 1536)]
        public ReadOnlyMemory<float> DefinitionEmbedding { get; set; }
    }
}