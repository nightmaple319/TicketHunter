using TicketHunter.Core.Services;

namespace TicketHunter.Web.Endpoints;

public static class BotEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/run", (BotEngine bot) =>
        {
            bot.Start();
            return Results.Ok(new { success = true, message = "Bot started" });
        });

        app.MapPost("/api/pause", (BotEngine bot) =>
        {
            bot.Pause();
            return Results.Ok(new { success = true, message = "Bot paused" });
        });

        app.MapPost("/api/resume", (BotEngine bot) =>
        {
            bot.Resume();
            return Results.Ok(new { success = true, message = "Bot resumed" });
        });

        app.MapPost("/api/stop", async (BotEngine bot) =>
        {
            await bot.StopAsync();
            return Results.Ok(new { success = true, message = "Bot stopped" });
        });

        app.MapGet("/api/status", (BotEngine bot) => Results.Ok(bot.Status));

        app.MapPost("/api/reset", async (BotEngine bot) =>
        {
            await bot.StopAsync();
            return Results.Ok(new { success = true, message = "Bot reset" });
        });

        app.MapGet("/api/question", (BotEngine bot) =>
        {
            return Results.Ok(new { question = bot.Status.CaptchaQuestion });
        });
    }
}
