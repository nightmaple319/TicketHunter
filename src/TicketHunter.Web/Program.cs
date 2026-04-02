using TicketHunter.Core.Browser;
using TicketHunter.Core.Platforms;
using TicketHunter.Core.Services;
using TicketHunter.Web.Endpoints;

namespace TicketHunter.Web;

public class WebHost
{
    public static WebApplication Build(string[] args, int port = 16888)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Register services
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<BrowserManager>();
        builder.Services.AddSingleton<CloudflareHandler>();
        builder.Services.AddSingleton<CookieManager>();
        builder.Services.AddSingleton<OcrService>(sp =>
        {
            var config = sp.GetRequiredService<ConfigService>().Config;
            var modelPath = config.Ocr.ModelPath;
            // Derive charset path from model path directory
            var modelDir = Path.GetDirectoryName(modelPath) ?? "assets";
            var charsetPath = Path.Combine(modelDir, "charset_beta.json");
            return new OcrService(sp.GetRequiredService<ILogger<OcrService>>(), modelPath, charsetPath);
        });
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<SoundService>();

        // Register platform handlers
        builder.Services.AddSingleton<IPlatformHandler, TixcraftHandler>();
        builder.Services.AddSingleton<IPlatformHandler, TicketmasterHandler>();

        // Register bot engine
        builder.Services.AddSingleton<BotEngine>();

        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        builder.WebHost.UseUrls($"http://localhost:{port}");

        var app = builder.Build();

        app.UseCors();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Map API endpoints
        ConfigEndpoints.Map(app);
        BotEndpoints.Map(app);
        ToolEndpoints.Map(app);

        return app;
    }
}
