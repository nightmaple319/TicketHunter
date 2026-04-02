using TicketHunter.Core.Services;

namespace TicketHunter.Web.Endpoints;

public static class ToolEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/test-discord", async (NotificationService notify, ConfigService config) =>
        {
            await notify.SendDiscordAsync(
                config.Config.Advanced.DiscordWebhookUrl,
                "🧪 TicketHunter test notification");
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/test-telegram", async (NotificationService notify, ConfigService config) =>
        {
            await notify.SendTelegramAsync(
                config.Config.Advanced.TelegramBotToken,
                config.Config.Advanced.TelegramChatId,
                "🧪 TicketHunter test notification");
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/ocr", async (HttpRequest request, OcrService ocr) =>
        {
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            var result = ocr.Recognize(ms.ToArray());
            return Results.Ok(new { result = result ?? "" });
        });
    }
}
