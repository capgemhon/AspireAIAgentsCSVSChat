using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using AspireAIAgentsCSVSChat.Web.Components;
using AspireAIAgentsCSVSChat.Web.Services;
using AspireAIAgentsCSVSChat.Web.Services.Ingestion;
using OpenAI;
using AspireAIAgentsCSVSChat.Web.Services.MultiAgents;
using AspireAIAgentsCSVSChat.Web.Services.Configuration;
using Aspire.Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using AspireAIAgentsCSVSChat.Web;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var openai = builder.AddAzureOpenAIClient("openai");
openai.AddChatClient("gpt-4o-mini")
   .UseFunctionInvocation()
   .UseOpenTelemetry(configure: c =>
       c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");

var vectorStore = new JsonVectorStore(Path.Combine(AppContext.BaseDirectory, "vector-store"));
builder.Services.AddSingleton<IVectorStore>(vectorStore);
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddSingleton<IServiceProvider>(sp => sp);
builder.AddSqliteDbContext<IngestionCacheDbContext>("ingestionCache");

// Fix for CS7036: Provide required parameters to the SimpleVectorDb constructor.  
// Fix for KMEXP03: Suppress the diagnostic warning for evaluation purposes.  

////var simpleVectorDBConfig = new SimpleVectorDbConfig
////{
////    StorageType = Microsoft.KernelMemory.FileSystem.DevTools.FileSystemTypes.Disk,
////    Directory = Path.Combine(AppContext.BaseDirectory, "simple-vector-store")
////};

////// Ensure the following line is correct and matches the extension method provided by the Azure OpenAI SDK.  

////builder.Services.AddSingleton<ITextEmbeddingGenerator>(embeddingGenerator);
////var textEmbeddingGenerator = builder.Services.BuildServiceProvider().GetRequiredService<ITextEmbeddingGenerator>();
////var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

////#pragma warning disable KMEXP03 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
////var simpleVectorDb = new SimpleVectorDb(simpleVectorDBConfig, textEmbeddingGenerator, loggerFactory);
////#pragma warning restore KMEXP03 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

////#pragma warning disable KMEXP03 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
////builder.Services.AddSingleton<SimpleVectorDb>(simpleVectorDb);
#pragma warning restore KMEXP03 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddSingleton<ITextEmbeddingGenerator>(sp => vectorStore as ITextEmbeddingGenerator);
builder.Services.AddSingleton<ITextGenerator>(sp => vectorStore as ITextGenerator);
#pragma warning disable KMEXP04 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddSingleton<InProcessPipelineOrchestrator>();
//builder.Services.AddSingleton<IDocumentStorage, DocumentStorageImplementation>();
#pragma warning restore KMEXP04 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

//new ConfigurationBuilder()
//    //.AddJsonFile("appsettings.json")
//    .AddJsonFile("appsettings.Development.json", optional: true)
//    .Build()
//    //.BindSection("KernelMemory:Services:OpenAI", openAIConfig)
//    //.BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
//    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

// Add MultiAgentApiClient Services  
builder.Services.AddSingleton<MultiAgentApiClient>();

var connectionString = builder.Configuration.GetConnectionString("openai");

// Add Logging   
builder.Services.AddLogging(builder => builder.AddConsole());

// Assume the connection string is formatted like: "Endpoint=https://api.openai.com/;ApiKey=xxxx"  
if (!string.IsNullOrWhiteSpace(connectionString))
{
    // Assume the connection string is formatted like: "Endpoint=https://api.openai.com/;ApiKey=xxxx"    
    var parameters = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var endpoint = parameters.FirstOrDefault(p => p.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
                                ?.Split('=')[1];
    var key = parameters.FirstOrDefault(p => p.StartsWith("Key=", StringComparison.OrdinalIgnoreCase))
                            ?.Split('=')[1];

    builder.Services.Configure<SemanticKernelServiceSettings>(options =>
    {
        options.AzureOpenAISettings.Endpoint = string.IsNullOrWhiteSpace(endpoint) ? null : new Uri(endpoint);
        options.AzureOpenAISettings.Key = key;
    });

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    builder.Services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
    {
        var endpoint1 = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint; // Use endpoint as a string directly  
        var apiKey1 = key;
        var model = "text-embedding-3-small";
        AzureOpenAIConfig azureOpenAIConfig = new AzureOpenAIConfig();
        azureOpenAIConfig.Endpoint = endpoint1;
        azureOpenAIConfig.APIKey = apiKey1;
        azureOpenAIConfig.Deployment = model;
#pragma warning disable KMEXP01 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return new AzureOpenAITextEmbeddingGenerationService(model, endpoint1, apiKey1);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore KMEXP01 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    });
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    // Add ITextEmbeddingGenerator service 
    builder.Services.AddSingleton<ITextEmbeddingGenerator>(sp =>
    {
        var endpoint1 = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint; // Use endpoint as a string directly  
        var apiKey1 = key;
        var model = "text-embedding-3-small";
        AzureOpenAIConfig azureOpenAIConfig = new AzureOpenAIConfig();
        azureOpenAIConfig.Endpoint = endpoint1;
        azureOpenAIConfig.APIKey = apiKey1;
        azureOpenAIConfig.Deployment = model;
#pragma warning disable KMEXP01 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return new AzureOpenAITextEmbeddingGenerator(azureOpenAIConfig);
#pragma warning restore KMEXP01 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    });
}

var app = builder.Build();
IngestionCacheDbContext.Initialize(app.Services);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.  
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.  
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from    
// other sources by implementing IIngestionSource.    
// Important: ensure that any content you ingest is trusted, as it may be reflected back  
// to users or could be a source of prompt injection risk.  
await DataIngestor.IngestDataAsync(
  app.Services,
  new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Uploads"))
);
await DataIngestor.IngestDataAsync(
   app.Services,
   new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data"))
);

app.Run();

