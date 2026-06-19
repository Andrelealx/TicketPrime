using Hangfire;
using Hangfire.MemoryStorage;
using RedCodeApi.Data;
using RedCodeApi.Endpoints;
using RedCodeApi.Services;
using RedCodeApi.Services.Scrapers;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ──────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5139")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Cache ─────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CacheService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CacheService>>();
    var memoryCache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    return new CacheService(logger, null, memoryCache);
});

// ── Normalizador ──────────────────────────────────
builder.Services.AddSingleton<NormalizadorDados>();

// ── Scrapers (Strategy Pattern) ───────────────────
void ConfigureScraperHttpClient<T>(IServiceCollection services, string name)
    where T : class, IVooScraper
{
    services.AddHttpClient<T>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd(
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(
            "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    services.AddScoped<IVooScraper>(sp => sp.GetRequiredService<T>());
}

ConfigureScraperHttpClient<ScraperLatam>(builder.Services, "latam");
ConfigureScraperHttpClient<ScraperGol>(builder.Services, "gol");
ConfigureScraperHttpClient<ScraperAzul>(builder.Services, "azul");
builder.Services.AddScoped<IVooScraper, ScraperDecolar>();
builder.Services.AddScoped<ScrapingScheduler>();

// ── Hangfire ──────────────────────────────────────
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// ── Connection String ─────────────────────────────
var connStr = builder.Configuration.GetConnectionString("RedCode")
    ?? throw new InvalidOperationException(
        "Connection string 'RedCode' nao configurada. " +
        "Defina em appsettings ou na variavel ConnectionStrings__RedCode.");

// ── Email (SMTP para alertas de preco — LOW-01) ─────
builder.Services.AddSingleton<EmailService>();

// ── Analisador de Preços (Motor de Regras + Score) ─
builder.Services.AddScoped<AnalisadorPrecosService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AnalisadorPrecosService>>();
    return new AnalisadorPrecosService(connStr, logger);
});

builder.Services.AddSingleton(connStr);

// ── Build & Middleware ────────────────────────────
var app = builder.Build();
app.UseCors("BlazorPolicy");

// ── Database ──────────────────────────────────────
DbInitializer.Initialize(connStr);

// ── Hangfire Dashboard & Jobs ─────────────────────
app.UseHangfireDashboard();
RecurringJob.AddOrUpdate<ScrapingScheduler>(
    "scraping-rotas-populares",
    scheduler => scheduler.AtualizarRotasPopulares(),
    "0 */6 * * *");
RecurringJob.AddOrUpdate<ScrapingScheduler>(
    "verificacao-alertas",
    scheduler => scheduler.VerificarAlertas(),
    "0 */2 * * *");

// ── Endpoints ─────────────────────────────────────
app.MapAeroportosEndpoints(connStr);
app.MapCompanhiasEndpoints(connStr);
app.MapRotasEndpoints(connStr);
app.MapVoosEndpoints(connStr);
app.MapAlertasEndpoints(connStr);
app.MapAnaliseEndpoints(connStr);

app.Run();
