using Microsoft.EntityFrameworkCore;
using MyRagChatBot.Data;
using MyRagChatBot.Services;
using MyRagChatBot.Components;

var builder = WebApplication.CreateBuilder(args);

// Program.cs
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddSingleton<MarkdownService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=MyRagChatBotDB;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Services
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IVectorDatabase, SqlVectorDatabase>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<RagService>();




// Replace ye lines:
builder.Services.AddHttpClient<OpenAIService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["OpenAI:BaseUrl"] ?? "https://openrouter.ai/api/v1/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config["OpenAI:ApiKey"]}");
    client.DefaultRequestHeaders.Add("X-Title", "MyRagChatBot");
});

// With ye lines:
builder.Services.AddHttpClient<OpenAIService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    // ABSOLUTE URL hona chahiye
    var baseUrl = config["OpenAI:BaseUrl"] ?? "https://openrouter.ai/api/v1/";
    client.BaseAddress = new Uri(baseUrl);

    var apiKey = config["OpenAI:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    client.DefaultRequestHeaders.Add("X-Title", "MyRagChatBot");
    client.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost:7031"); // Optional
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ✅ .NET 8 routing
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// DB create
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
