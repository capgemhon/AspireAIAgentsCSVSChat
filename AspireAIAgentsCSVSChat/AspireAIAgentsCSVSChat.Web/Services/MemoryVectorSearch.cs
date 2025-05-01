using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;

namespace AspireAIAgentsCSVSChat.Web.Services
{
    public class MemoryVectorSearch
    {

        /// <summary>
        /// Example showing how to ingest data into an in-memory vector store.
        /// </summary>
        public async Task IngestDataIntoInMemoryVectorStoreAsync(Kernel _kernel, IServiceProvider serviceProvider)
        {
            // Construct the vector store and get the collection.
            var vectorStore = new InMemoryVectorStore();
            var collection = vectorStore.GetCollection<string, Glossary>("softwareglossary");

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
                Term = "SOUP",
                Definition = "As per IEC62304 ;Software Of Unknown Provenance (or Pedigree). SOFTWARE ITEM \r\nthat is already developed and generally available and that has not been developed for \r\nthe purpose of being incorporated into the MEDICAL DEVICE."
            };

            yield return new Glossary
            {
                Key = "2",
                Category = "Software",
                Term = "OTS",
                Definition = "As per IEC62304 ;“off the-shelf software “or SOFTWARE ITEM previously developed for \r\nwhich adequate records of the development PROCESSES are not available. Means it \r\ncannot be claimed that the medical device software lifecycle controls are followed also \r\nparticular type of SOUP."
            };

            yield return new Glossary
            {
                Key = "3",
                Category = "Software",
                Term = "COTS",
                Definition = "COTS software that comes from a commercial supplier.\r\n"
            };

            yield return new Glossary
            {
                Key = "4",
                Category = "Software",
                Term = "Software",
                Definition = "As per FDA ;confirmation by examination and provision of objective evidence that \r\nsoftware specifications conform to user needs and intended uses, and that the \r\nparticular requirements implemented through software can be consistently fulfilled."
            };
        }

        /// <summary>
        /// Search the given collection for the most relevant result to the given search string.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="searchString">The string to search matches for.</param>
        /// <param name="textEmbeddingGenerationService">The service to generate embeddings with.</param>
        /// <returns>The top search result.</returns>
        public async Task<String> SearchAnInMemoryVectorStoreAsync(IServiceProvider serviceProvider, string query)
        {
            var textEmbeddingGenerationService = serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>();
            var collection = await GetVectorStoreCollectionWithDataAsync(serviceProvider);

            // Search the vector store.
            var searchResultItem = await SearchVectorStoreAsync(
                collection,
                query,
                textEmbeddingGenerationService);

            // Write the search result with its score to the console.
            Console.WriteLine(searchResultItem.Record.Definition);

            // Write the search result with its score to the console and infer the result into the output.
            Console.WriteLine($"Definition: {searchResultItem.Record.Definition}");
            Console.WriteLine($"Score: {searchResultItem.Score}");
            var inferredResult = $"The search result is '{searchResultItem.Record.Definition}' with a relevance score of {searchResultItem.Score}.";
            Console.WriteLine($"Inferred Output: {inferredResult}");
            Console.WriteLine(searchResultItem.Score);

            return "Definition: " + searchResultItem.Record.Definition + " " + inferredResult;
        }

        /// <summary>
        /// Search the given collection for the most relevant result to the given search string.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="searchString">The string to search matches for.</param>
        /// <param name="textEmbeddingGenerationService">The service to generate embeddings with.</param>
        /// <returns>The top search result.</returns>
        internal static async Task<VectorSearchResult<Glossary>> SearchVectorStoreAsync(IVectorStoreRecordCollection<string, Glossary> collection, string searchString, ITextEmbeddingGenerationService textEmbeddingGenerationService)
        {
            // Generate an embedding from the search string.
            var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);

            // Search the store and get the single most relevant result.
            var searchResult = await collection.VectorizedSearchAsync(
                searchVector,
                new()
                {
                    Top = 1
                });
            var searchResultItems = await searchResult.Results.ToListAsync();
            return searchResultItems.First();
        }

        /// <summary>
        /// Do a more complex vector search with pre-filtering.
        /// </summary>
        private async Task<IVectorStoreRecordCollection<string, Glossary>> GetVectorStoreCollectionWithDataAsync(IServiceProvider serviceProvider)
        {
            // Construct the vector store and get the collection.
            var vectorStore = new InMemoryVectorStore();
            var collection = vectorStore.GetCollection<string, Glossary>("softwareglossary");

            var textEmbeddingGenerationService = serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>();

            // Ingest data into the collection using the code from step 1.
            await IngestDataIntoVectorStoreAsync(collection, textEmbeddingGenerationService);

            return collection;
        }
    }
}
