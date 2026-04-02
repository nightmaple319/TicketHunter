namespace TicketHunter.Core.Models;

public enum BotState
{
    Idle,
    Running,
    Paused,
    WaitingForCaptcha,
    OrderCompleted,
    Error
}

public class BotStatus
{
    public BotState State { get; set; } = BotState.Idle;
    public string Message { get; set; } = "";
    public string CurrentUrl { get; set; } = "";
    public string CaptchaQuestion { get; set; } = "";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
