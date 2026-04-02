using System.Text.Json.Serialization;

namespace TicketHunter.Core.Models;

public class AppConfig
{
    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = "";

    [JsonPropertyName("ticket_number")]
    public int TicketNumber { get; set; } = 2;

    [JsonPropertyName("browser")]
    public string Browser { get; set; } = "chrome";

    [JsonPropertyName("date_auto_select")]
    public AutoSelectConfig DateAutoSelect { get; set; } = new();

    [JsonPropertyName("area_auto_select")]
    public AreaSelectConfig AreaAutoSelect { get; set; } = new();

    [JsonPropertyName("ocr")]
    public OcrConfig Ocr { get; set; } = new();

    [JsonPropertyName("accounts")]
    public AccountsConfig Accounts { get; set; } = new();

    [JsonPropertyName("contact")]
    public ContactConfig Contact { get; set; } = new();

    [JsonPropertyName("advanced")]
    public AdvancedConfig Advanced { get; set; } = new();
}

public class AutoSelectConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = "";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "from_top";
}

public class AreaSelectConfig : AutoSelectConfig
{
    [JsonPropertyName("keyword_exclude")]
    public string KeywordExclude { get; set; } = "";
}

public class OcrConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("force_submit")]
    public bool ForceSubmit { get; set; } = false;

    [JsonPropertyName("model_path")]
    public string ModelPath { get; set; } = "assets/ocr_models/custom.onnx";

    [JsonPropertyName("image_source")]
    public string ImageSource { get; set; } = "canvas";
}

public class AccountsConfig
{
    [JsonPropertyName("tixcraft_sid")]
    public string TixcraftSid { get; set; } = "";

    [JsonPropertyName("ticketmaster_cookie")]
    public string TicketmasterCookie { get; set; } = "";
}

public class ContactConfig
{
    [JsonPropertyName("real_name")]
    public string RealName { get; set; } = "";

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("credit_card_prefix")]
    public string CreditCardPrefix { get; set; } = "";
}

public class AdvancedConfig
{
    [JsonPropertyName("play_sound")]
    public bool PlaySound { get; set; } = true;

    [JsonPropertyName("headless")]
    public bool Headless { get; set; } = false;

    [JsonPropertyName("auto_reload_interval")]
    public int AutoReloadInterval { get; set; } = 3;

    [JsonPropertyName("max_retry")]
    public int MaxRetry { get; set; } = 3;

    [JsonPropertyName("discord_webhook_url")]
    public string DiscordWebhookUrl { get; set; } = "";

    [JsonPropertyName("telegram_bot_token")]
    public string TelegramBotToken { get; set; } = "";

    [JsonPropertyName("telegram_chat_id")]
    public string TelegramChatId { get; set; } = "";

    [JsonPropertyName("proxy_server")]
    public string ProxyServer { get; set; } = "";

    [JsonPropertyName("web_port")]
    public int WebPort { get; set; } = 16888;

    [JsonPropertyName("auto_submit_ticket")]
    public bool AutoSubmitTicket { get; set; } = true;

    [JsonPropertyName("schedule_start")]
    public string ScheduleStart { get; set; } = "";

    [JsonPropertyName("idle_keyword")]
    public string IdleKeyword { get; set; } = "";

    [JsonPropertyName("resume_keyword")]
    public string ResumeKeyword { get; set; } = "";

    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; } = false;
}
