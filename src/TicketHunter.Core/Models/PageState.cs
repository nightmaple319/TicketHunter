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
    Login,

    // --- 新增狀態 ---
    QueueIT,            // iBon Queue-IT 排隊頁面
    Booking,            // KKTIX #/booking 訂購頁
    SeatSelection,      // KHAM / NianDai / UDN 座位選擇頁
    EmailVerification,  // Cityline Email 驗證
    Checkout,           // 結帳/付款頁（通用）
    TicketType,         // KKTIX / TicketPlus 票種選擇頁
    PopupBlocking       // 彈窗阻擋中（iBon 等）
}
