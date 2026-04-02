namespace TicketHunter.Core.Models;

public class TicketInfo
{
    public string EventName { get; set; } = "";
    public string Date { get; set; } = "";
    public string Area { get; set; } = "";
    public int Quantity { get; set; }
    public string Price { get; set; } = "";
    public PlatformType Platform { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
