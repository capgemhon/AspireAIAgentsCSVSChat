using Microsoft.Extensions.VectorData;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Prompts;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel;
using Microsoft.KernelMemory.AI;

namespace AspireAIAgentsCSVSChat.Web
{
    public class CSVSPromptBuilder : IPromptProvider
    {
        private const string VerificationPrompt = """
                                              Facts:
                                              {{$facts}}
                                              ======
                                              Given only the facts above, verify the fact below.
                                              You don't know where the knowledge comes from, just answer.
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
                    return this._fallbackProvider.ReadPrompt(promptName);
            }
        }
    

      /// <summary>
    /// Example showing how to ingest data into an in-memory vector store.
    /// </summary>
        public async Task IngestDataIntoInMemoryVectorStoreAsync(Kernel _kernel, IServiceProvider serviceProvider)
        {
            // Construct the vector store and get the collection.
            var vectorStore = new InMemoryVectorStore();
            var collection = vectorStore.GetCollection<string, Glossary>("skglossary");

            // Suppress the diagnostic warning SKEXP0001 for the usage of ITextEmbeddingGenerationService.  
#pragma warning disable SKEXP0001
            var textEmbeddingGenerationService = serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>();

            // Ingest data into the collection.
            await IngestDataIntoVectorStoreAsync(collection, textEmbeddingGenerationService);

            // Retrieve an item from the collection and write it to the console.
            var record = await collection.GetAsync("4");
            Console.WriteLine(record!.Definition);
        }

        /// <summary>
        /// Ingest data into the given collection.
        /// </summary>
        /// <param name="collection">The collection to ingest data into.</param>
        /// <param name="textEmbeddingGenerationService">The service to use for generating embeddings.</param>
        /// <returns>The keys of the upserted records.</returns>
        internal static async Task<IEnumerable<string>> IngestDataIntoVectorStoreAsync(
            IVectorStoreRecordCollection<string, Glossary> collection,
            ITextEmbeddingGenerationService textEmbeddingGenerationService)
        {
            // Create the collection if it doesn't exist.
            await collection.CreateCollectionIfNotExistsAsync();

            // Create glossary entries and generate embeddings for them.
            var glossaryEntries = CreateGlossaryEntries().ToList();
            var tasks = glossaryEntries.Select(entry => Task.Run(async () =>
            {
                entry.DefinitionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(entry.Definition);
            }));
            await Task.WhenAll(tasks);

            // Upsert the glossary entries into the collection and return their keys.
            var upsertedKeysTasks = glossaryEntries.Select(x => collection.UpsertAsync(x));
            return await Task.WhenAll(upsertedKeysTasks);
        }

        /// <summary>
        /// Create some sample glossary entries.
        /// </summary>
        /// <returns>A list of sample glossary entries.</returns>
        private static IEnumerable<Glossary> CreateGlossaryEntries()
        {
            yield return new Glossary
            {
                Key = "1",
                Category = "Software",
                Term = "API",
                Definition = "Application Programming Interface. A set of rules and specifications that allow software components to communicate and exchange data."
            };

            yield return new Glossary
            {
                Key = "2",
                Category = "Software",
                Term = "SDK",
                Definition = "Software development kit. A set of libraries and tools that allow software developers to build software more easily."
            };

            yield return new Glossary
            {
                Key = "3",
                Category = "SK",
                Term = "Connectors",
                Definition = "Semantic Kernel Connectors allow software developers to integrate with various services providing AI capabilities, including LLM, AudioToText, TextToAudio, Embedding generation, etc."
            };

            yield return new Glossary
            {
                Key = "4",
                Category = "SK",
                Term = "Semantic Kernel",
                Definition = "Semantic Kernel is a set of libraries that allow software developers to more easily develop applications that make use of AI experiences."
            };

            yield return new Glossary
            {
                Key = "5",
                Category = "AI",
                Term = "RAG",
                Definition = "Retrieval Augmented Generation - a term that refers to the process of retrieving additional data to provide as context to an LLM to use when generating a response (completion) to a user’s question (prompt)."
            };

            yield return new Glossary
            {
                Key = "6",
                Category = "AI",
                Term = "LLM",
                Definition = "Large language model. A type of artificial ingelligence algorithm that is designed to understand and generate human language."
            };
        }
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