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
builder.AddSqliteDbContext<IngestionCacheDbContext>("ingestionCache");

// Add MultiAgentApiClient Services
builder.Services.AddSingleton<MultiAgentApiClient>();
var connectionString = builder.Configuration.GetConnectionString("openai");

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
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
