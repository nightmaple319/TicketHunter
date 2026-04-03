using System.Text.Json;
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

    [JsonPropertyName("kktix")]
    public KktixConfig Kktix { get; set; } = new();

    [JsonPropertyName("tixcraft")]
    public TixcraftConfig Tixcraft { get; set; } = new();
}

// ============================================================
// Date / Area Auto-Select
// ============================================================

public class AutoSelectConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = "";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "from_top";

    /// <summary>
    /// 當 keyword 無匹配時是否自動 fallback 選擇第一個可用項目。
    /// false = 嚴格模式（不選）
    /// </summary>
    [JsonPropertyName("auto_fallback")]
    public bool AutoFallback { get; set; } = true;
}

public class AreaSelectConfig : AutoSelectConfig
{
    [JsonPropertyName("keyword_exclude")]
    public string KeywordExclude { get; set; } = "";
}

// ============================================================
// OCR
// ============================================================

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

    /// <summary>使用 beta charset（charset_beta.json）</summary>
    [JsonPropertyName("beta")]
    public bool Beta { get; set; } = false;

    /// <summary>使用自訓練 universal 模型而非 ddddocr 官方模型</summary>
    [JsonPropertyName("use_universal")]
    public bool UseUniversal { get; set; } = true;
}

// ============================================================
// Accounts — 所有平台帳號 / Cookie
// ============================================================

public class AccountsConfig
{
    // --- Tixcraft 家族 ---
    [JsonPropertyName("tixcraft_sid")]
    public string TixcraftSid { get; set; } = "";

    [JsonPropertyName("indievox_sid")]
    public string IndievoxSid { get; set; } = "";

    // --- Ticketmaster ---
    [JsonPropertyName("ticketmaster_cookie")]
    public string TicketmasterCookie { get; set; } = "";

    // --- KKTIX ---
    [JsonPropertyName("kktix_account")]
    public string KktixAccount { get; set; } = "";

    [JsonPropertyName("kktix_password")]
    public string KktixPassword { get; set; } = "";

    // --- iBon ---
    [JsonPropertyName("ibon_cookie")]
    public string IBonCookie { get; set; } = "";

    // --- TicketPlus (遠大) ---
    [JsonPropertyName("ticketplus_account")]
    public string TicketPlusAccount { get; set; } = "";

    [JsonPropertyName("ticketplus_password")]
    public string TicketPlusPassword { get; set; } = "";

    // --- KHAM (寬宏) ---
    [JsonPropertyName("kham_account")]
    public string KhamAccount { get; set; } = "";

    [JsonPropertyName("kham_password")]
    public string KhamPassword { get; set; } = "";

    // --- NianDai (年代) ---
    [JsonPropertyName("ticket_account")]
    public string NianDaiAccount { get; set; } = "";

    [JsonPropertyName("ticket_password")]
    public string NianDaiPassword { get; set; } = "";

    // --- UDN ---
    [JsonPropertyName("udn_account")]
    public string UdnAccount { get; set; } = "";

    [JsonPropertyName("udn_password")]
    public string UdnPassword { get; set; } = "";

    // --- FamiTicket ---
    [JsonPropertyName("fami_account")]
    public string FamiAccount { get; set; } = "";

    [JsonPropertyName("fami_password")]
    public string FamiPassword { get; set; } = "";

    // --- FunOne ---
    [JsonPropertyName("funone_session_cookie")]
    public string FunOneSessionCookie { get; set; } = "";

    // --- FANSI GO ---
    [JsonPropertyName("fansigo_cookie")]
    public string FansiGoCookie { get; set; } = "";

    [JsonPropertyName("fansigo_account")]
    public string FansiGoAccount { get; set; } = "";

    [JsonPropertyName("fansigo_password")]
    public string FansiGoPassword { get; set; } = "";

    // --- Cityline (香港) ---
    [JsonPropertyName("cityline_account")]
    public string CitylineAccount { get; set; } = "";

    // --- URBTIX (香港) ---
    [JsonPropertyName("urbtix_account")]
    public string UrbtixAccount { get; set; } = "";

    [JsonPropertyName("urbtix_password")]
    public string UrbtixPassword { get; set; } = "";

    // --- HKTicketing (香港) ---
    [JsonPropertyName("hkticketing_account")]
    public string HkTicketingAccount { get; set; } = "";

    [JsonPropertyName("hkticketing_password")]
    public string HkTicketingPassword { get; set; } = "";

    // --- Facebook (跨平台社群登入) ---
    [JsonPropertyName("facebook_account")]
    public string FacebookAccount { get; set; } = "";

    [JsonPropertyName("facebook_password")]
    public string FacebookPassword { get; set; } = "";
}

// ============================================================
// Contact (結帳自動填寫)
// ============================================================

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

// ============================================================
// Sound (分離 ticket / order 音效)
// ============================================================

/// <summary>
/// 向後相容轉換器：接受舊格式 bool（true/false）或新格式 object。
/// bool true → Ticket=true, Order=true；bool false → Ticket=false, Order=false。
/// </summary>
public class PlaySoundConfigConverter : JsonConverter<PlaySoundConfig>
{
    public override PlaySoundConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
        {
            var enabled = reader.GetBoolean();
            return new PlaySoundConfig { Ticket = enabled, Order = enabled };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<PlaySoundConfigDto>(ref reader, options)?.ToConfig()
                   ?? new PlaySoundConfig();
        }

        throw new JsonException($"Unexpected token for play_sound: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, PlaySoundConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("ticket", value.Ticket);
        writer.WriteBoolean("order", value.Order);
        writer.WriteString("filename", value.Filename);
        writer.WriteEndObject();
    }

    /// <summary>Internal DTO to avoid infinite recursion during deserialization.</summary>
    private class PlaySoundConfigDto
    {
        [JsonPropertyName("ticket")]
        public bool Ticket { get; set; } = true;

        [JsonPropertyName("order")]
        public bool Order { get; set; } = true;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";

        public PlaySoundConfig ToConfig() => new() { Ticket = Ticket, Order = Order, Filename = Filename };
    }
}

[JsonConverter(typeof(PlaySoundConfigConverter))]
public class PlaySoundConfig
{
    /// <summary>搶到票時播放音效</summary>
    public bool Ticket { get; set; } = true;

    /// <summary>訂單完成時播放音效</summary>
    public bool Order { get; set; } = true;

    /// <summary>自訂音效檔路徑（空字串 = 使用預設）</summary>
    public string Filename { get; set; } = "";
}

// ============================================================
// Advanced
// ============================================================

public class AdvancedConfig
{
    [JsonPropertyName("play_sound")]
    public PlaySoundConfig PlaySound { get; set; } = new();

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

    // --- 新增：過熱保護 ---
    /// <summary>連續刷新幾次後觸發冷卻</summary>
    [JsonPropertyName("auto_reload_overheat_count")]
    public int AutoReloadOverheatCount { get; set; } = 4;

    /// <summary>冷卻等待秒數</summary>
    [JsonPropertyName("auto_reload_overheat_cd")]
    public int AutoReloadOverheatCd { get; set; } = 1;

    // --- 新增：瀏覽器管理 ---
    /// <summary>定時重啟瀏覽器（秒），0 = 不重啟</summary>
    [JsonPropertyName("reset_browser_interval")]
    public int ResetBrowserInterval { get; set; } = 0;

    /// <summary>瀏覽器視窗大小，格式 "寬,高"</summary>
    [JsonPropertyName("window_size")]
    public string WindowSize { get; set; } = "1366,768";

    // --- 新增：效能優化 ---
    /// <summary>阻擋字型/圖示/圖片以加速載入</summary>
    [JsonPropertyName("hide_some_image")]
    public bool HideSomeImage { get; set; } = false;

    /// <summary>阻擋 Facebook/fbcdn 追蹤腳本</summary>
    [JsonPropertyName("block_facebook_network")]
    public bool BlockFacebookNetwork { get; set; } = false;

    // --- 新增：座位相關 ---
    /// <summary>允許非相鄰座位（iBon, NianDai, KHAM, Ticketmaster）</summary>
    [JsonPropertyName("disable_adjacent_seat")]
    public bool DisableAdjacentSeat { get; set; } = false;

    // --- 新增：驗證碼輔助 ---
    /// <summary>預設驗證碼答案字典（分號分隔，如 "答案1;答案2"）</summary>
    [JsonPropertyName("user_guess_string")]
    public string UserGuessString { get; set; } = "";

    /// <summary>自動猜測驗證碼答案（數學、地理等）</summary>
    [JsonPropertyName("auto_guess_options")]
    public bool AutoGuessOptions { get; set; } = true;

    // --- 新增：優惠碼 ---
    /// <summary>優惠碼/會員碼（支援 KKTIX, TicketPlus）</summary>
    [JsonPropertyName("discount_code")]
    public string DiscountCode { get; set; } = "";
}

// ============================================================
// KKTIX 平台專屬設定
// ============================================================

public class KktixConfig
{
    /// <summary>自動按下「下一步」按鈕</summary>
    [JsonPropertyName("auto_press_next_step")]
    public bool AutoPressNextStep { get; set; } = true;

    /// <summary>自動填入票數</summary>
    [JsonPropertyName("auto_fill_ticket_number")]
    public bool AutoFillTicketNumber { get; set; } = true;

    /// <summary>KKTIX 訂購頁最大停留時間（秒），超時自動送出</summary>
    [JsonPropertyName("max_dwell_time")]
    public int MaxDwellTime { get; set; } = 90;
}

// ============================================================
// Tixcraft 平台專屬設定
// ============================================================

public class TixcraftConfig
{
    /// <summary>自動跳過已售完場次</summary>
    [JsonPropertyName("pass_date_is_sold_out")]
    public bool PassDateIsSoldOut { get; set; } = true;

    /// <summary>自動重新整理「即將開賣」頁面</summary>
    [JsonPropertyName("auto_reload_coming_soon")]
    public bool AutoReloadComingSoon { get; set; } = true;
}
