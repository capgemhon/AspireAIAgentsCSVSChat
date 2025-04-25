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
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

//// Add logging to the application
//builder.Services.AddSingleton<ILogger<SemanticKernelService>>(sp =>
//   sp.GetRequiredService<ILoggerFactory>().CreateLogger<SemanticKernelService>());

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

// Registering embedding generation service with a service collection.
var services = new ServiceCollection();
//#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddOpenAITextEmbeddingGeneration("text-embedding-3-small");
//#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Add MultiAgentApiClient Services
builder.Services.AddSingleton<MultiAgentApiClient>();

var connectionString = builder.Configuration.GetConnectionString("openai");

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
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
