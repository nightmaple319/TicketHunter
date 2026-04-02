using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TicketHunter.Core.Services;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly HttpClient _httpClient;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task SendDiscordAsync(string webhookUrl, string message)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl)) return;

        try
        {
            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != "https" ||
                !uri.Host.EndsWith("discord.com"))
            {
                _logger.LogWarning("Invalid Discord webhook URL");
                return;
            }

            var payload = JsonSerializer.Serialize(new { content = message });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Discord notification sent");
            else
                _logger.LogWarning("Discord notification failed: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord notification");
        }
    }

    public async Task SendTelegramAsync(string botToken, string chatId, string message)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId)) return;

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = JsonSerializer.Serialize(new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "HTML"
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Telegram notification sent");
            else
                _logger.LogWarning("Telegram notification failed: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram notification");
        }
    }

    public async Task NotifyAllAsync(Models.AdvancedConfig config, string message)
    {
        var tasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
            tasks.Add(SendDiscordAsync(config.DiscordWebhookUrl, message));

        if (!string.IsNullOrWhiteSpace(config.TelegramBotToken))
            tasks.Add(SendTelegramAsync(config.TelegramBotToken, config.TelegramChatId, message));

        await Task.WhenAll(tasks);
    }
}
