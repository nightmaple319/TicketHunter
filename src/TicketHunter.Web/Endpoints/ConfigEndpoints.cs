using TicketHunter.Core.Models;
using TicketHunter.Core.Services;

namespace TicketHunter.Web.Endpoints;

public static class ConfigEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/load", (ConfigService config) => Results.Ok(config.Config));

        app.MapPost("/api/save", (AppConfig newConfig, ConfigService config) =>
        {
            config.Save(newConfig);
            return Results.Ok(new { success = true });
        });

        app.MapGet("/api/version", () => Results.Ok(new
        {
            version = "1.0.0",
            name = "TicketHunter",
            framework = "C# .NET 8"
        }));
    }
}
