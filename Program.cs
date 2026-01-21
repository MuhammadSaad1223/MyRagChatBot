using Microsoft.EntityFrameworkCore;
using MyRagChatBot.Data;
using MyRagChatBot.Services;
using MyRagChatBot.Components;

var builder = WebApplication.CreateBuilder(args);

// Render uses PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Program.cs
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
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<IGeminiAIService, GeminiAIService>();
builder.Services.AddHttpClient<GeminiAIService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    // Gemini API Base URL
    var baseUrl = config["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/";
    client.BaseAddress = new Uri(baseUrl);

    client.DefaultRequestHeaders.Add("User-Agent", "MyRagChatBot/1.0");
});

builder.Services.AddScoped<IVectorDatabase, SqlVectorDatabase>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<RagService>();

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