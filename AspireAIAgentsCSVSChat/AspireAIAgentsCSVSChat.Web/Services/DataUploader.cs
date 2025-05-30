﻿using AspireAIAgentsCSVSChat.Web.Services.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;

namespace AspireAIAgentsCSVSChat.Web.Services
{
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class DataUploader(IVectorStore vectorStore, ITextEmbeddingGenerationService textEmbeddingGenerationService)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        /// <summary>
        /// Generate an embedding for each text paragraph and upload it to the specified collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection to upload the text paragraphs to.</param>
        /// <param name="textParagraphs">The text paragraphs to upload.</param>
        /// <returns>An async task.</returns>
        public async Task GenerateEmbeddingsAndUpload(string collectionName, IEnumerable<TextParagraph> textParagraphs)
        {
            var collection = vectorStore.GetCollection<string, TextParagraph>(collectionName);
            await collection.CreateCollectionIfNotExistsAsync();

            foreach (var paragraph in textParagraphs)
            {
                // Generate the text embedding.
                Console.WriteLine($"Generating embedding for paragraph: {paragraph.ParagraphId}");
                paragraph.TextEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(paragraph.Text);

                // Upload the text paragraph.
                Console.WriteLine($"Upserting paragraph: {paragraph.ParagraphId}");
                await collection.UpsertAsync(paragraph);

                Console.WriteLine();
            }
        }

    }
}
