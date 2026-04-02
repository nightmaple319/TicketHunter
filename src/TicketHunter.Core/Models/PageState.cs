namespace TicketHunter.Core.Models;

public enum PageState
{
    Unknown,
    EventList,
    DateSelection,
    AreaSelection,
    QuantityAndCaptcha,
    SoldOut,
    ComingSoon,
    Queue,
    OrderComplete,
    CloudflareChallenge,
    Verify,
    Login
}
